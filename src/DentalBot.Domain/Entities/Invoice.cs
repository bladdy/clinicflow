using DentalBot.Domain.Common;

namespace DentalBot.Domain.Entities;

public class Invoice : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid SubscriptionId { get; set; }
    public CompanySubscription Subscription { get; set; } = null!;
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total => Amount + Tax;
    public string Currency { get; set; } = "MXN";
    public DateTime InvoiceDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public bool IsPaid { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
}
