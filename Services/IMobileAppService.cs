using DocAttestation.Models;

namespace DocAttestation.Services;

public interface IMobileAppService
{
    /// <summary>
    /// Register a new device for QR scanning
    /// </summary>
    Task<DeviceRegistrationResult> RegisterDeviceAsync(DeviceRegistrationRequest request, string userId);
    
    /// <summary>
    /// Validate device token and signature
    /// </summary>
    Task<DeviceValidationResult> ValidateDeviceAsync(string deviceToken, string signature, string timestamp, string nonce);
    
    /// <summary>
    /// Verify QR code from mobile app (secured endpoint)
    /// </summary>
    Task<SecureVerificationResult> VerifyQRFromAppAsync(SecureVerificationRequest request);
    
    /// <summary>
    /// Revoke a registered device
    /// </summary>
    Task<bool> RevokeDeviceAsync(int deviceId, string reason, string adminUserId);
    
    /// <summary>
    /// Get all devices for a user
    /// </summary>
    Task<List<RegisteredDevice>> GetUserDevicesAsync(string userId);
    
    /// <summary>
    /// Generate HMAC signature for validation
    /// </summary>
    string GenerateSignature(string data, string timestamp, string nonce);
    
    /// <summary>
    /// Validate HMAC signature from app
    /// </summary>
    bool ValidateSignature(string data, string signature, string timestamp, string nonce);
}

public class DeviceRegistrationRequest
{
    public string DeviceId { get; set; } = null!;
    public string DeviceName { get; set; } = null!;
    public string Platform { get; set; } = null!;
    public string OSVersion { get; set; } = null!;
    public string AppVersion { get; set; } = null!;
    public string AppSignature { get; set; } = null!; // App integrity signature
}

public class DeviceRegistrationResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? DeviceToken { get; set; }
    public DateTime? TokenExpiry { get; set; }
}

public class DeviceValidationResult
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public RegisteredDevice? Device { get; set; }
}

public class SecureVerificationRequest
{
    public string QRToken { get; set; } = null!;
    public string DeviceToken { get; set; } = null!;
    public string Signature { get; set; } = null!;
    public string Timestamp { get; set; } = null!;
    public string Nonce { get; set; } = null!;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}

public class SecureVerificationResult
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public string? ApplicantName { get; set; }
    public string? DocumentType { get; set; }
    public string? IssuingAuthority { get; set; }
    public DateTime? AttestationDate { get; set; }
    public string? ApplicationNumber { get; set; }
    public string? ApplicantPhoto { get; set; } // Base64 encoded
    public string? VerifiedBy { get; set; }
}

