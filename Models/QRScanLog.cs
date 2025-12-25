namespace DocAttestation.Models;

public class QRScanLog
{
    public int Id { get; set; }
    public int QRVerificationTokenId { get; set; }
    public DateTime ScanTime { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public bool IsValid { get; set; }
    public string? ValidationResult { get; set; }
    
    // Navigation
    public virtual QRVerificationToken QRVerificationToken { get; set; } = null!;
}

