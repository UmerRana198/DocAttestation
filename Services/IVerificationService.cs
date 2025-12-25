using DocAttestation.Models;

namespace DocAttestation.Services;

public interface IVerificationService
{
    Task<VerificationResult> VerifyQRTokenAsync(string encryptedToken, string? ipAddress = null, string? userAgent = null);
}

public class VerificationResult
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public string? IssuingAuthority { get; set; }
    public DateTime? AttestationDate { get; set; }
    public string? DocumentType { get; set; }
}

