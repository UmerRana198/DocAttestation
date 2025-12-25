namespace DocAttestation.Models;

public class Payment
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public decimal Amount { get; set; }
    public string CardNumber { get; set; } = null!; // Last 4 digits only for security
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public string? TransactionId { get; set; }
    public string? PaymentMethod { get; set; } // Card, Bank Transfer, etc.
    
    // Navigation
    public virtual Application Application { get; set; } = null!;
}

public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Refunded = 3
}

