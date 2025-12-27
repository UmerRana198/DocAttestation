using DocAttestation.Configuration;
using DocAttestation.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DocAttestation.Controllers.Api;

/// <summary>
/// Secure API endpoints for mobile app QR verification
/// These endpoints ONLY work with registered mobile app devices
/// </summary>
[ApiController]
[Route("api/mobile")]
public class MobileVerificationController : ControllerBase
{
    private readonly IMobileAppService _mobileAppService;
    private readonly MobileAppSettings _settings;
    private readonly ILogger<MobileVerificationController> _logger;

    public MobileVerificationController(
        IMobileAppService mobileAppService,
        IOptions<MobileAppSettings> settings,
        ILogger<MobileVerificationController> logger)
    {
        _mobileAppService = mobileAppService;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Register a device for QR scanning (requires officer authentication)
    /// </summary>
    [HttpPost("device/register")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "VerificationOfficer,Supervisor,AttestationOfficer,Admin")]
    public async Task<IActionResult> RegisterDevice([FromBody] DeviceRegistrationRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new { success = false, message = "Invalid request data" });
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { success = false, message = "User not authenticated" });
        }

        var result = await _mobileAppService.RegisterDeviceAsync(request, userId);

        if (!result.Success)
        {
            return BadRequest(new
            {
                success = false,
                message = result.Message
            });
        }

        return Ok(new
        {
            success = true,
            message = result.Message,
            deviceToken = result.DeviceToken,
            tokenExpiry = result.TokenExpiry,
            serverTime = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Verify QR code from mobile app (secured with device token and HMAC signature)
    /// </summary>
    [HttpPost("verify")]
    [AllowAnonymous] // Device token + signature provides authentication
    public async Task<IActionResult> VerifyQR([FromBody] SecureVerificationRequest request)
    {
        // Validate required headers
        var appVersion = Request.Headers["X-App-Version"].FirstOrDefault();
        var platform = Request.Headers["X-Platform"].FirstOrDefault();

        if (string.IsNullOrEmpty(appVersion) || string.IsNullOrEmpty(platform))
        {
            _logger.LogWarning("Missing app headers from IP: {IP}", GetClientIP());
            return BadRequest(new
            {
                isValid = false,
                message = "Invalid request - missing app identification"
            });
        }

        // Update device IP
        if (request.DeviceToken != null)
        {
            // IP tracking happens inside the service
        }

        var result = await _mobileAppService.VerifyQRFromAppAsync(request);

        if (!result.IsValid)
        {
            return Ok(new
            {
                isValid = false,
                message = result.Message
            });
        }

        return Ok(new
        {
            isValid = true,
            message = result.Message,
            applicantName = result.ApplicantName,
            documentType = result.DocumentType,
            issuingAuthority = result.IssuingAuthority,
            attestationDate = result.AttestationDate,
            applicationNumber = result.ApplicationNumber,
            applicantPhoto = result.ApplicantPhoto,
            verifiedBy = result.VerifiedBy,
            verifiedAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Get device status
    /// </summary>
    [HttpGet("device/status")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "VerificationOfficer,Supervisor,AttestationOfficer,Admin")]
    public async Task<IActionResult> GetDeviceStatus()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var devices = await _mobileAppService.GetUserDevicesAsync(userId);

        return Ok(new
        {
            success = true,
            devices = devices.Select(d => new
            {
                id = d.Id,
                deviceName = d.DeviceName,
                platform = d.Platform,
                appVersion = d.AppVersion,
                isActive = d.IsActive,
                isRevoked = d.IsRevoked,
                registeredAt = d.RegisteredAt,
                lastUsedAt = d.LastUsedAt,
                scanCount = d.ScanCount,
                tokenExpiry = d.TokenExpiry
            })
        });
    }

    /// <summary>
    /// Deactivate a device
    /// </summary>
    [HttpPost("device/{deviceId}/deactivate")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "VerificationOfficer,Supervisor,AttestationOfficer,Admin")]
    public async Task<IActionResult> DeactivateDevice(int deviceId)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var result = await _mobileAppService.RevokeDeviceAsync(deviceId, "Deactivated by user", userId);

        return Ok(new { success = result });
    }

    /// <summary>
    /// Health check endpoint for app to verify server connectivity
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult HealthCheck()
    {
        var request = HttpContext.Request;
        var baseUrl = _settings.BaseApiUrl ?? $"{request.Scheme}://{request.Host}";
        
        return Ok(new
        {
            status = "healthy",
            serverTime = DateTime.UtcNow,
            minimumAppVersion = _settings.MinimumAppVersion,
            allowWebVerification = _settings.AllowWebVerification,
            baseApiUrl = baseUrl,
            endpoints = new
            {
                health = $"{baseUrl}/api/mobile/health",
                login = $"{baseUrl}/api/auth/login",
                refresh = $"{baseUrl}/api/auth/refresh",
                registerDevice = $"{baseUrl}/api/mobile/device/register",
                verify = $"{baseUrl}/api/mobile/verify"
            }
        });
    }

    private string GetClientIP()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

