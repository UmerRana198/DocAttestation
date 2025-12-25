using DocAttestation.Models;

namespace DocAttestation.Services;

public interface IQRCodeService
{
    Task<string> GenerateQRTokenAsync(int applicationId, string documentHash);
    string GenerateQRCodeImage(string encryptedToken);
    Task<QRTokenData?> DecryptQRTokenAsync(string encryptedToken);
}

public class QRTokenData
{
    public int ApplicationId { get; set; }
    public string DocumentHash { get; set; } = null!;
    public DateTime IssuedAt { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string Nonce { get; set; } = null!;
}

