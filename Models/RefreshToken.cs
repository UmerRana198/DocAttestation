using Microsoft.AspNetCore.Identity;

namespace DocAttestation.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? RevokedByIp { get; set; }
    public DateTime? RevokedAt { get; set; }
    public bool IsRevoked => RevokedAt != null;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsActive => !IsRevoked && !IsExpired;
    
    // Navigation
    public virtual ApplicationUser User { get; set; } = null!;
}

