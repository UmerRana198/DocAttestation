namespace DocAttestation.Configuration;

public class QRCodeSettings
{
    public string VerificationBaseUrl { get; set; } = string.Empty;
    public int TokenExpirationDays { get; set; } = 365;
    public int QRSize { get; set; } = 200;
}

