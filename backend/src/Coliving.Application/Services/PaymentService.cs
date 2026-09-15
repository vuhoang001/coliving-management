using Coliving.Application.Common;
using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Coliving.Domain.Entities;
using Coliving.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Coliving.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IAppDbContext _db;
    private readonly INotificationService _notify;
    private readonly VnPaySettings _vnpay;
    public PaymentService(IAppDbContext db, INotificationService notify, IOptions<VnPaySettings> vnpay)
    {
        _db = db;
        _notify = notify;
        _vnpay = vnpay.Value;
    }

    public async Task<PaymentResultDto> PayAsync(int actorId, PayInvoiceDto dto)
    {
        var inv = await LoadInvoice(dto.InvoiceId);
        if (!Enum.TryParse<PaymentMethod>(dto.Method, true, out var method))
            throw new AppException("Phương thức thanh toán không hợp lệ.");
        if (inv.Status is InvoiceStatus.Draft)
            throw new AppException("Hoá đơn chưa phát hành, không thể thanh toán.");
        if (inv.Status is InvoiceStatus.Paid or InvoiceStatus.Cancelled)
            throw new AppException("Hoá đơn đã thanh toán hoặc đã huỷ.");

        var amount = ResolveAmount(inv, actorId, dto.Amount);
        var payment = ApplyPayment(inv, actorId, amount, method, dto.TransactionRef, PaymentStatus.Paid);
        MarkSharePaid(inv, actorId);
        await _db.SaveChangesAsync();

        await _notify.NotifyRoleAsync(UserRole.Manager, "Đã nhận thanh toán",
            $"Hoá đơn {inv.InvoiceNumber}: +{amount:N0} VNĐ ({method}).", "invoice", $"/invoices/{inv.Id}");

        return Result(inv, amount, $"Đã ghi nhận thanh toán {amount:N0} VNĐ.");
    }

    public async Task<CreatePaymentDto> CreateVnPayUrlAsync(int actorId, int invoiceId, string ipAddress)
    {
        var inv = await LoadInvoice(invoiceId);
        if (inv.Status is InvoiceStatus.Paid or InvoiceStatus.Cancelled or InvoiceStatus.Draft)
            throw new AppException("Hoá đơn không ở trạng thái có thể thanh toán online.");

        var amount = ResolveAmount(inv, actorId, null);

        // Chưa cấu hình credential VNPAY thật → dùng chế độ giả lập: trả URL trang mock ở frontend.
        if (_vnpay.IsMock)
        {
            var sep = _vnpay.MockUrl.Contains('?') ? '&' : '?';
            var mockUrl = $"{_vnpay.MockUrl}{sep}invoiceId={inv.Id}&amount={(long)amount}";
            return new CreatePaymentDto { PaymentUrl = mockUrl, IsMock = true };
        }

        // Giờ Việt Nam (GMT+7) — VNPAY yêu cầu định dạng yyyyMMddHHmmss.
        var now = DateTime.UtcNow.AddHours(7);
        var vnp = new VnPayLibrary();
        vnp.AddRequestData("vnp_Version", _vnpay.Version);
        vnp.AddRequestData("vnp_Command", "pay");
        vnp.AddRequestData("vnp_TmnCode", _vnpay.TmnCode);
        // VNPAY tính theo đơn vị nhỏ nhất → nhân 100.
        vnp.AddRequestData("vnp_Amount", ((long)(amount * 100)).ToString());
        vnp.AddRequestData("vnp_CurrCode", "VND");
        vnp.AddRequestData("vnp_TxnRef", inv.Id.ToString());
        vnp.AddRequestData("vnp_OrderInfo", $"Thanh toan hoa don {inv.InvoiceNumber}");
        vnp.AddRequestData("vnp_OrderType", "other");
        vnp.AddRequestData("vnp_Locale", _vnpay.Locale);
        vnp.AddRequestData("vnp_ReturnUrl", _vnpay.ReturnUrl);
        vnp.AddRequestData("vnp_IpAddr", string.IsNullOrWhiteSpace(ipAddress) ? "127.0.0.1" : ipAddress);
        vnp.AddRequestData("vnp_CreateDate", now.ToString("yyyyMMddHHmmss"));
        vnp.AddRequestData("vnp_ExpireDate", now.AddMinutes(15).ToString("yyyyMMddHHmmss"));

        return new CreatePaymentDto { PaymentUrl = vnp.CreateRequestUrl(_vnpay.BaseUrl, _vnpay.HashSecret), IsMock = false };
    }

    public async Task<PaymentResultDto> HandleVnPayReturnAsync(IReadOnlyDictionary<string, string> query, bool isIpn = false)
    {
        var vnp = new VnPayLibrary();
        foreach (var (key, value) in query)
            if (key.StartsWith("vnp_")) vnp.AddResponseData(key, value);

        var secureHash = query.TryGetValue("vnp_SecureHash", out var h) ? h : string.Empty;
        var responseCode = vnp.GetResponseData("vnp_ResponseCode");
        var transactionStatus = vnp.GetResponseData("vnp_TransactionStatus");
        var txnRef = vnp.GetResponseData("vnp_TxnRef");
        var transactionNo = vnp.GetResponseData("vnp_TransactionNo");

        // 1) Xác thực chữ ký chống giả mạo.
        if (!vnp.ValidateSignature(secureHash, _vnpay.HashSecret))
            return Fail(null, "97", "Chữ ký không hợp lệ.");

        if (!int.TryParse(txnRef, out var invoiceId))
            return Fail(null, "01", "Không xác định được hoá đơn.");

        var inv = await _db.Invoices.Include(i => i.Shares).Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);
        if (inv is null)
            return Fail(invoiceId, "01", "Không tìm thấy hoá đơn.");

        // 2) Idempotent — nếu đã thanh toán đủ thì trả lại kết quả thành công.
        if (inv.Status == InvoiceStatus.Paid)
            return Ok(inv, 0, "Hoá đơn đã được thanh toán.", isIpn ? "02" : "00");

        var outstanding = inv.Total - inv.PaidAmount;

        // 3) Đối chiếu số tiền để tránh sai lệch.
        var expected = (long)(outstanding * 100);
        if (!long.TryParse(vnp.GetResponseData("vnp_Amount"), out var paidAmount) || paidAmount != expected)
        {
            await MarkFailedAsync(inv);
            return Fail(invoiceId, "04", "Số tiền thanh toán không khớp.");
        }

        // 4) "00" ở cả hai field mới coi là giao dịch thành công.
        if (responseCode == "00" && transactionStatus == "00")
        {
            await ApplySuccessAsync(inv, outstanding, transactionNo);
            return Ok(inv, outstanding, "Thanh toán VNPAY thành công.");
        }

        await MarkFailedAsync(inv);
        return Fail(invoiceId, responseCode, "Giao dịch không thành công hoặc bị huỷ.");
    }

    public async Task<PaymentResultDto> CompleteMockAsync(int actorId, int invoiceId, bool success)
    {
        if (!_vnpay.IsMock)
            throw new AppException("Chế độ giả lập đã tắt (đang dùng VNPAY thật).");

        var inv = await LoadInvoice(invoiceId);
        if (inv.Status is InvoiceStatus.Cancelled or InvoiceStatus.Draft)
            throw new AppException("Hoá đơn không ở trạng thái có thể thanh toán online.");
        if (inv.Status == InvoiceStatus.Paid)
            return Ok(inv, 0, "Hoá đơn đã được thanh toán.");

        var outstanding = inv.Total - inv.PaidAmount;
        if (success)
        {
            await ApplySuccessAsync(inv, outstanding, $"MOCK{DateTime.UtcNow:yyMMddHHmmss}");
            return Ok(inv, outstanding, "Thanh toán (giả lập) thành công.");
        }
        await MarkFailedAsync(inv);
        return Fail(invoiceId, "24", "Bạn đã huỷ giao dịch (giả lập).");
    }

    // ---- helpers ----

    private async Task<Invoice> LoadInvoice(int id) =>
        await _db.Invoices.Include(i => i.Shares).Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == id)
        ?? throw AppException.NotFound("Không tìm thấy hoá đơn.");

    /// <summary>Số tiền cần trả: nếu người trả là 1 người ở ghép → phần chia chưa trả của họ; ngược lại → dư nợ hoá đơn.</summary>
    private static decimal ResolveAmount(Invoice inv, int actorId, decimal? requested)
    {
        var outstanding = inv.Total - inv.PaidAmount;
        if (requested.HasValue)
            return requested.Value <= 0 || requested.Value > outstanding
                ? throw new AppException("Số tiền thanh toán không hợp lệ.")
                : requested.Value;

        var share = inv.Shares.FirstOrDefault(s => s.TenantId == actorId && !s.IsPaid);
        if (share != null) return Math.Min(share.ShareAmount, outstanding);
        return outstanding;
    }

    private Payment ApplyPayment(Invoice inv, int paidById, decimal amount, PaymentMethod method,
        string? txnRef, PaymentStatus status)
    {
        var payment = new Payment
        {
            InvoiceId = inv.Id, PaidById = paidById, Amount = amount, Method = method,
            Status = status, TransactionRef = txnRef, PaidAt = DateTime.UtcNow
        };
        _db.Payments.Add(payment);

        inv.PaidAmount += amount;
        inv.Status = inv.PaidAmount >= inv.Total ? InvoiceStatus.Paid : InvoiceStatus.PartiallyPaid;
        return payment;
    }

    private static void MarkSharePaid(Invoice inv, int actorId)
    {
        var share = inv.Shares.FirstOrDefault(s => s.TenantId == actorId && !s.IsPaid);
        if (share != null) { share.IsPaid = true; share.PaidAt = DateTime.UtcNow; }
    }

    /// <summary>Ghi nhận thanh toán VNPAY thành công: tạo Payment, đánh dấu các phần chia đã trả, cập nhật trạng thái.</summary>
    private async Task ApplySuccessAsync(Invoice inv, decimal amount, string transactionNo)
    {
        ApplyPayment(inv, inv.TenantId, amount, PaymentMethod.VnPay, transactionNo, PaymentStatus.Paid);
        foreach (var s in inv.Shares.Where(s => !s.IsPaid)) { s.IsPaid = true; s.PaidAt = DateTime.UtcNow; }
        await _db.SaveChangesAsync();

        await _notify.NotifyUserAsync(inv.TenantId, "Thanh toán thành công",
            $"Hoá đơn {inv.InvoiceNumber} đã thanh toán {amount:N0} VNĐ qua VNPAY.", "invoice", $"/invoices/{inv.Id}");
        await _notify.NotifyRoleAsync(UserRole.Manager, "Đã nhận thanh toán VNPAY",
            $"Hoá đơn {inv.InvoiceNumber}: +{amount:N0} VNĐ (VNPAY).", "invoice", $"/invoices/{inv.Id}");
    }

    private async Task MarkFailedAsync(Invoice inv)
    {
        // Ghi nhận một giao dịch VNPAY thất bại (không đổi PaidAmount/Status hoá đơn).
        _db.Payments.Add(new Payment
        {
            InvoiceId = inv.Id, PaidById = inv.TenantId, Amount = 0,
            Method = PaymentMethod.VnPay, Status = PaymentStatus.Failed, PaidAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    private PaymentResultDto Ok(Invoice inv, decimal paid, string message, string responseCode = "00") => new()
    {
        Success = true, Message = message, InvoiceId = inv.Id,
        InvoiceStatus = inv.Status.ToString(), PaidAmount = paid,
        ResponseCode = responseCode,
        // RedirectUrl luôn báo "success" cho trang FE (mã "02" chỉ là quy ước IPN với VNPAY, không phải lỗi).
        RedirectUrl = BuildRedirect(true, inv.Id, "00")
    };

    private PaymentResultDto Result(Invoice inv, decimal paid, string message) => new()
    {
        Success = true, Message = message, InvoiceId = inv.Id,
        InvoiceStatus = inv.Status.ToString(), PaidAmount = paid
    };

    private PaymentResultDto Fail(int? invoiceId, string code, string message) => new()
    {
        Success = false, Message = message, InvoiceId = invoiceId ?? 0,
        InvoiceStatus = string.Empty, PaidAmount = 0,
        ResponseCode = code,
        RedirectUrl = BuildRedirect(false, invoiceId, code)
    };

    /// <summary>Ghép URL trang kết quả bên frontend kèm tham số để hiển thị.</summary>
    private string BuildRedirect(bool success, int? invoiceId, string code)
    {
        var sep = _vnpay.FrontendReturnUrl.Contains('?') ? '&' : '?';
        var status = success ? "success" : "failed";
        var iid = invoiceId?.ToString() ?? string.Empty;
        return $"{_vnpay.FrontendReturnUrl}{sep}status={status}&invoiceId={Uri.EscapeDataString(iid)}" +
               $"&code={Uri.EscapeDataString(code)}";
    }
}
