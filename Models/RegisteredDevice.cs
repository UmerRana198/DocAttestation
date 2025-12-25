namespace DocAttestation.Models;

/// <summary>
/// Represents a registered mobile device authorized to scan QR codes
/// </summary>
public class RegisteredDevice
{
    public int Id { get; set; }
    
    /// <summary>
    /// User who registered this device (Officer)
    /// </summary>
    public string UserId { get; set; } = null!;
    
    /// <summary>
    /// Unique device identifier (from device hardware)
    /// </summary>
    public string DeviceId { get; set; } = null!;
    
    /// <summary>
    /// Device name for display purposes
    /// </summary>
    public string DeviceName { get; set; } = null!;
    
    /// <summary>
    /// Platform: Android, iOS, Windows
    /// </summary>
    public string Platform { get; set; } = null!;
    
    /// <summary>
    /// OS version
    /// </summary>
    public string OSVersion { get; set; } = null!;
    
    /// <summary>
    /// App version installed
    /// </summary>
    public string AppVersion { get; set; } = null!;
    
    /// <summary>
    /// Secure token for this device
    /// </summary>
    public string DeviceToken { get; set; } = null!;
    
    /// <summary>
    /// Hash of device token for lookup
    /// </summary>
    public string DeviceTokenHash { get; set; } = null!;
    
    /// <summary>
    /// Token expiry date
    /// </summary>
    public DateTime TokenExpiry { get; set; }
    
    /// <summary>
    /// Whether device is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Whether device is revoked (security concern)
    /// </summary>
    public bool IsRevoked { get; set; } = false;
    
    /// <summary>
    /// Reason for revocation
    /// </summary>
    public string? RevocationReason { get; set; }
    
    /// <summary>
    /// When device was registered
    /// </summary>
    public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Last time device was used for verification
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
    
    /// <summary>
    /// Last known IP address
    /// </summary>
    public string? LastIpAddress { get; set; }
    
    /// <summary>
    /// Total scan count from this device
    /// </summary>
    public int ScanCount { get; set; } = 0;
    
    // Navigation
    public virtual ApplicationUser User { get; set; } = null!;
}

