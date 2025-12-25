namespace DocAttestation.Models;

public class QRVerificationToken
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string EncryptedToken { get; set; } = null!;
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiryDate { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string? RevokedReason { get; set; }
    public DateTime? RevokedAt { get; set; }
    
    // Navigation
    public virtual Application Application { get; set; } = null!;
    public virtual ICollection<QRScanLog> ScanLogs { get; set; } = new List<QRScanLog>();
}

