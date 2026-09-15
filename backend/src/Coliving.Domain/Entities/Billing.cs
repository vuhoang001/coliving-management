using Coliving.Domain.Common;
using Coliving.Domain.Enums;

namespace Coliving.Domain.Entities;

/// <summary>Hoá đơn kỳ (tiền phòng + điện nước + dịch vụ...), có thể chia cho nhiều người ở ghép.</summary>
public class Invoice : BaseEntity
{
    public string InvoiceNumber { get; set; } = default!;  // INV-xxxxxx
    public int TenantId { get; set; }
    public int? RoomId { get; set; }
    public int? ContractId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime IssueDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
    public decimal PaidAmount { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Draft;
    public string? Note { get; set; }

    // ---- Chỉ số công tơ + giảm giá/thuế ----
    public int? PreviousElectricityReading { get; set; }
    public int? CurrentElectricityReading { get; set; }
    public int? PreviousWaterReading { get; set; }
    public int? CurrentWaterReading { get; set; }
    public decimal Discount { get; set; }
    public decimal Tax { get; set; }

    public User Tenant { get; set; } = default!;
    public Room? Room { get; set; }
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public ICollection<InvoiceShare> Shares { get; set; } = new List<InvoiceShare>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

/// <summary>Một dòng chi phí trên hoá đơn.</summary>
public class InvoiceItem : BaseEntity
{
    public int InvoiceId { get; set; }
    public InvoiceItemType Type { get; set; } = InvoiceItemType.Other;
    public string Description { get; set; } = default!;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
    public string? Unit { get; set; }      // Đơn vị: kWh | m³ | tháng | lần...
    public string? Note { get; set; }

    public Invoice Invoice { get; set; } = default!;
}

/// <summary>Phần chia hoá đơn cho từng người ở ghép (chia tiền phòng/dịch vụ).</summary>
public class InvoiceShare : BaseEntity
{
    public int InvoiceId { get; set; }
    public int TenantId { get; set; }
    public decimal ShareAmount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }

    public Invoice Invoice { get; set; } = default!;
    public User Tenant { get; set; } = default!;
}

/// <summary>Giao dịch thanh toán cho một hoá đơn.</summary>
public class Payment : BaseEntity
{
    public int InvoiceId { get; set; }
    public int PaidById { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? TransactionRef { get; set; }
    public DateTime? PaidAt { get; set; }

    public Invoice Invoice { get; set; } = default!;
    public User PaidBy { get; set; } = default!;
}
