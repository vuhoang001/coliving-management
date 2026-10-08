using Coliving.Application.DTOs;
using Coliving.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Coliving.API.Controllers;

[Authorize]
[EnableRateLimiting("payment")]
public class PaymentsController : BaseApiController
{
    private readonly IPaymentService _service;
    public PaymentsController(IPaymentService service) => _service = service;

    /// <summary>Ghi nhận thanh toán trực tiếp (tiền mặt / chuyển khoản) — hoặc khách tự trả phần chia của mình.</summary>
    [HttpPost("pay")]
    public async Task<ActionResult<PaymentResultDto>> Pay(PayInvoiceDto dto)
        => Ok(await _service.PayAsync(CurrentUserId, dto));

    /// <summary>Tạo URL thanh toán VNPAY (mock nếu chưa cấu hình credential thật).</summary>
    [HttpPost("vnpay/{invoiceId:int}")]
    public async Task<ActionResult<CreatePaymentDto>> CreateVnPay(int invoiceId)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        return Ok(await _service.CreateVnPayUrlAsync(CurrentUserId, invoiceId, ip));
    }

    /// <summary>VNPAY gọi lại sau khi thanh toán. Verify chữ ký rồi redirect khách về frontend.</summary>
    [AllowAnonymous]
    [HttpGet("vnpay/return")]
    public async Task<IActionResult> VnPayReturn()
    {
        var query = HttpContext.Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        var result = await _service.HandleVnPayReturnAsync(query);
        return Redirect(result.RedirectUrl!);
    }

    /// <summary>
    /// IPN — VNPAY gọi server-to-server để chốt kết quả, độc lập với việc khách có quay lại hay không.
    /// Đây mới là nguồn tin cậy: dù khách rớt mạng lúc redirect, hoá đơn vẫn được cập nhật.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("vnpay/ipn")]
    public async Task<IActionResult> VnPayIpn()
    {
        var query = HttpContext.Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());
        var result = await _service.HandleVnPayReturnAsync(query, isIpn: true);
        // VNPAY yêu cầu phản hồi JSON { RspCode, Message }.
        return Ok(new { RspCode = result.ResponseCode, Message = result.Message });
    }

    /// <summary>Trang giả lập gọi để chốt kết quả khi chưa có credential VNPAY thật.</summary>
    [HttpPost("vnpay/mock/{invoiceId:int}")]
    public async Task<ActionResult<PaymentResultDto>> CompleteMock(int invoiceId, [FromQuery] bool success = true)
        => Ok(await _service.CompleteMockAsync(CurrentUserId, invoiceId, success));
}
