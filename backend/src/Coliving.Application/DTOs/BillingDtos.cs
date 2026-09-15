using System.ComponentModel.DataAnnotations;

namespace Coliving.Application.DTOs;

public record InvoiceItemDto
{
    public int Id { get; init; }
    public string Type { get; init; } = default!;
    public string Description { get; init; } = default!;
    public decimal Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal Amount { get; init; }
    public string? Unit { get; init; }
    public string? Note { get; init; }
}

public record InvoiceShareDto
{
    public int Id { get; init; }
    public int TenantId { get; init; }
    public string TenantName { get; init; } = default!;
    public decimal ShareAmount { get; init; }
    public bool IsPaid { get; init; }
    public DateTime? PaidAt { get; init; }
}

public record PaymentDto
{
    public int Id { get; init; }
    public decimal Amount { get; init; }
    public string Method { get; init; } = default!;
    public string Status { get; init; } = default!;
    public string? TransactionRef { get; init; }
    public DateTime? PaidAt { get; init; }
}

public record InvoiceDto
{
    public int Id { get; init; }
    public string InvoiceNumber { get; init; } = default!;
    public int TenantId { get; init; }
    public string TenantName { get; init; } = default!;
    public int? RoomId { get; init; }
    public string? RoomCode { get; init; }
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }
    public DateTime IssueDate { get; init; }
    public DateTime DueDate { get; init; }
    public decimal Subtotal { get; init; }
    public decimal Total { get; init; }
    public decimal PaidAmount { get; init; }
    public string Status { get; init; } = default!;
    public string? Note { get; init; }
    public int? PreviousElectricityReading { get; init; }
    public int? CurrentElectricityReading { get; init; }
    public int? PreviousWaterReading { get; init; }
    public int? CurrentWaterReading { get; init; }
    public decimal Discount { get; init; }
    public decimal Tax { get; init; }
    public List<InvoiceItemDto> Items { get; init; } = new();
    public List<InvoiceShareDto> Shares { get; init; } = new();
    public List<PaymentDto> Payments { get; init; } = new();
}

public record CreateInvoiceItemDto
{
    public string Type { get; init; } = "Other";
    [Required] public string Description { get; init; } = default!;
    public decimal Quantity { get; init; } = 1;
    public decimal UnitPrice { get; init; }
    public string? Unit { get; init; }
    public string? Note { get; init; }
}

public record CreateInvoiceDto
{
    [Required] public int TenantId { get; init; }
    public int? RoomId { get; init; }
    public int? ContractId { get; init; }
    [Required] public DateTime PeriodStart { get; init; }
    [Required] public DateTime PeriodEnd { get; init; }
    public DateTime? DueDate { get; init; }
    public string? Note { get; init; }
    public int? PreviousElectricityReading { get; init; }
    public int? CurrentElectricityReading { get; init; }
    public int? PreviousWaterReading { get; init; }
    public int? CurrentWaterReading { get; init; }
    public decimal Discount { get; init; }
    public decimal Tax { get; init; }
    [Required, MinLength(1)] public List<CreateInvoiceItemDto> Items { get; init; } = new();
}

/// <summary>Một phần chia hoá đơn cho một người ở ghép.</summary>
public record InvoiceSplitPart
{
    [Required] public int TenantId { get; init; }
    /// <summary>Bỏ trống nếu muốn chia đều tự động.</summary>
    public decimal? Amount { get; init; }
}

public record SplitInvoiceDto
{
    /// <summary>Danh sách người ở ghép cùng chịu hoá đơn. Nếu Amount trống → chia đều.</summary>
    [Required, MinLength(1)] public List<InvoiceSplitPart> Parts { get; init; } = new();
}

public record PayInvoiceDto
{
    [Required] public int InvoiceId { get; init; }
    public decimal? Amount { get; init; }
    /// <summary>Cash | BankTransfer | VnPay</summary>
    public string Method { get; init; } = "Cash";
    public string? TransactionRef { get; init; }
}

public record PaymentResultDto
{
    public bool Success { get; init; }
    public string Message { get; init; } = default!;
    public int InvoiceId { get; init; }
    public string InvoiceStatus { get; init; } = default!;
    public decimal PaidAmount { get; init; }
    public string? ResponseCode { get; init; }
    public string? RedirectUrl { get; init; }
}

public record CreatePaymentDto
{
    public string PaymentUrl { get; init; } = default!;
    public bool IsMock { get; init; }
}
