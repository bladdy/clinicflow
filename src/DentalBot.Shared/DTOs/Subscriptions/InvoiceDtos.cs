namespace DentalBot.Shared.DTOs.Subscriptions;

public class InvoiceDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Tax { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime? PaidDate { get; set; }
    public bool IsPaid { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }
}
