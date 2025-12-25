namespace DocAttestation.Configuration;

public class MobileAppSettings
{
    /// <summary>
    /// Secret key shared between server and mobile app for HMAC signature
    /// Must be 64 characters for HMAC-SHA256
    /// </summary>
    public string AppSecret { get; set; } = null!;
    
    /// <summary>
    /// Unique identifier for the mobile app (Bundle ID / Package Name)
    /// </summary>
    public string AppIdentifier { get; set; } = null!;
    
    /// <summary>
    /// Minimum required app version
    /// </summary>
    public string MinimumAppVersion { get; set; } = "1.0.0";
    
    /// <summary>
    /// Token validity in minutes for device registration
    /// </summary>
    public int DeviceTokenValidityMinutes { get; set; } = 43200; // 30 days
    
    /// <summary>
    /// Maximum devices per user
    /// </summary>
    public int MaxDevicesPerUser { get; set; } = 3;
    
    /// <summary>
    /// Enable/disable web-based QR verification (should be false for security)
    /// </summary>
    public bool AllowWebVerification { get; set; } = false;
}

