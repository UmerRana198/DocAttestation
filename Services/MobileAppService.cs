using DocAttestation.Configuration;
using DocAttestation.Data;
using DocAttestation.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace DocAttestation.Services;

public class MobileAppService : IMobileAppService
{
    private readonly ApplicationDbContext _context;
    private readonly MobileAppSettings _settings;
    private readonly IEncryptionService _encryptionService;
    private readonly IQRCodeService _qrCodeService;
    private readonly ILogger<MobileAppService> _logger;

    public MobileAppService(
        ApplicationDbContext context,
        IOptions<MobileAppSettings> settings,
        IEncryptionService encryptionService,
        IQRCodeService qrCodeService,
        ILogger<MobileAppService> logger)
    {
        _context = context;
        _settings = settings.Value;
        _encryptionService = encryptionService;
        _qrCodeService = qrCodeService;
        _logger = logger;
    }

    public async Task<DeviceRegistrationResult> RegisterDeviceAsync(DeviceRegistrationRequest request, string userId)
    {
        try
        {
            // Validate app version
            if (!IsVersionValid(request.AppVersion))
            {
                return new DeviceRegistrationResult
                {
                    Success = false,
                    Message = $"App version {request.AppVersion} is outdated. Minimum required: {_settings.MinimumAppVersion}"
                };
            }

            // Validate app signature (integrity check)
            if (!ValidateAppSignature(request.AppSignature, request.DeviceId))
            {
                _logger.LogWarning("Invalid app signature from device {DeviceId}", request.DeviceId);
                return new DeviceRegistrationResult
                {
                    Success = false,
                    Message = "App signature validation failed. Please use the official app."
                };
            }

            // Check existing device count for user
            var existingDeviceCount = await _context.RegisteredDevices
                .CountAsync(d => d.UserId == userId && d.IsActive && !d.IsRevoked);

            if (existingDeviceCount >= _settings.MaxDevicesPerUser)
            {
                return new DeviceRegistrationResult
                {
                    Success = false,
                    Message = $"Maximum device limit ({_settings.MaxDevicesPerUser}) reached. Please deactivate an existing device."
                };
            }

            // Check if device already registered
            var existingDevice = await _context.RegisteredDevices
                .FirstOrDefaultAsync(d => d.DeviceId == request.DeviceId && d.UserId == userId);

            if (existingDevice != null)
            {
                if (existingDevice.IsRevoked)
                {
                    return new DeviceRegistrationResult
                    {
                        Success = false,
                        Message = "This device has been revoked for security reasons."
                    };
                }

                // Refresh token for existing device
                var newToken = GenerateSecureToken();
                var tokenHash = ComputeHash(newToken);

                existingDevice.DeviceToken = _encryptionService.Encrypt(newToken);
                existingDevice.DeviceTokenHash = tokenHash;
                existingDevice.TokenExpiry = DateTime.UtcNow.AddMinutes(_settings.DeviceTokenValidityMinutes);
                existingDevice.AppVersion = request.AppVersion;
                existingDevice.IsActive = true;

                await _context.SaveChangesAsync();

                return new DeviceRegistrationResult
                {
                    Success = true,
                    Message = "Device re-registered successfully",
                    DeviceToken = newToken,
                    TokenExpiry = existingDevice.TokenExpiry
                };
            }

            // Register new device
            var deviceToken = GenerateSecureToken();
            var deviceTokenHash = ComputeHash(deviceToken);

            var device = new RegisteredDevice
            {
                UserId = userId,
                DeviceId = request.DeviceId,
                DeviceName = request.DeviceName,
                Platform = request.Platform,
                OSVersion = request.OSVersion,
                AppVersion = request.AppVersion,
                DeviceToken = _encryptionService.Encrypt(deviceToken),
                DeviceTokenHash = deviceTokenHash,
                TokenExpiry = DateTime.UtcNow.AddMinutes(_settings.DeviceTokenValidityMinutes),
                IsActive = true,
                RegisteredAt = DateTime.UtcNow
            };

            _context.RegisteredDevices.Add(device);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Device {DeviceName} registered for user {UserId}", request.DeviceName, userId);

            return new DeviceRegistrationResult
            {
                Success = true,
                Message = "Device registered successfully",
                DeviceToken = deviceToken,
                TokenExpiry = device.TokenExpiry
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device for user {UserId}", userId);
            return new DeviceRegistrationResult
            {
                Success = false,
                Message = "An error occurred during device registration"
            };
        }
    }

    public async Task<DeviceValidationResult> ValidateDeviceAsync(string deviceToken, string signature, string timestamp, string nonce)
    {
        try
        {
            // Validate timestamp (prevent replay attacks - 5 minute window)
            if (!long.TryParse(timestamp, out var timestampTicks))
            {
                return new DeviceValidationResult { IsValid = false, Message = "Invalid timestamp format" };
            }

            var requestTime = new DateTime(timestampTicks, DateTimeKind.Utc);
            var timeDiff = Math.Abs((DateTime.UtcNow - requestTime).TotalMinutes);

            if (timeDiff > 5)
            {
                return new DeviceValidationResult { IsValid = false, Message = "Request timestamp expired" };
            }

            // Validate signature
            if (!ValidateSignature(deviceToken, signature, timestamp, nonce))
            {
                _logger.LogWarning("Invalid signature for device token");
                return new DeviceValidationResult { IsValid = false, Message = "Invalid signature" };
            }

            // Find device by token hash
            var tokenHash = ComputeHash(deviceToken);
            var device = await _context.RegisteredDevices
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.DeviceTokenHash == tokenHash);

            if (device == null)
            {
                return new DeviceValidationResult { IsValid = false, Message = "Device not registered" };
            }

            if (!device.IsActive)
            {
                return new DeviceValidationResult { IsValid = false, Message = "Device is deactivated" };
            }

            if (device.IsRevoked)
            {
                return new DeviceValidationResult { IsValid = false, Message = "Device has been revoked" };
            }

            if (device.TokenExpiry < DateTime.UtcNow)
            {
                return new DeviceValidationResult { IsValid = false, Message = "Device token expired. Please re-register." };
            }

            return new DeviceValidationResult
            {
                IsValid = true,
                Message = "Device validated successfully",
                Device = device
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating device");
            return new DeviceValidationResult { IsValid = false, Message = "Validation error" };
        }
    }

    public async Task<SecureVerificationResult> VerifyQRFromAppAsync(SecureVerificationRequest request)
    {
        try
        {
            // First validate the device
            var deviceValidation = await ValidateDeviceAsync(
                request.DeviceToken,
                request.Signature,
                request.Timestamp,
                request.Nonce);

            if (!deviceValidation.IsValid)
            {
                return new SecureVerificationResult
                {
                    IsValid = false,
                    Message = deviceValidation.Message
                };
            }

            var device = deviceValidation.Device!;

            // Decrypt and validate QR token
            var tokenData = await _qrCodeService.DecryptQRTokenAsync(request.QRToken);
            if (tokenData == null)
            {
                return new SecureVerificationResult
                {
                    IsValid = false,
                    Message = "Invalid or expired QR code"
                };
            }

            // Get application with details
            var application = await _context.Applications
                .Include(a => a.ApplicantProfile)
                .FirstOrDefaultAsync(a => a.Id == tokenData.ApplicationId);

            if (application == null)
            {
                return new SecureVerificationResult
                {
                    IsValid = false,
                    Message = "Application not found"
                };
            }

            // Validate application status
            if (application.Status != ApplicationStatus.Approved)
            {
                return new SecureVerificationResult
                {
                    IsValid = false,
                    Message = "Document is not attested"
                };
            }

            // Check if QR is revoked
            if (application.IsQRRevoked)
            {
                return new SecureVerificationResult
                {
                    IsValid = false,
                    Message = "This QR code has been revoked"
                };
            }

            // Verify document hash
            var expectedHash = application.StampedDocumentHash ?? application.DocumentHash;
            if (tokenData.DocumentHash != expectedHash)
            {
                return new SecureVerificationResult
                {
                    IsValid = false,
                    Message = "Document integrity check failed - possible tampering detected"
                };
            }

            // Check expiry
            if (tokenData.ExpiryDate < DateTime.UtcNow)
            {
                return new SecureVerificationResult
                {
                    IsValid = false,
                    Message = "QR code has expired"
                };
            }

            // Update device usage stats
            device.LastUsedAt = DateTime.UtcNow;
            device.ScanCount++;

            // Log the scan
            var qrToken = await _context.QRVerificationTokens
                .FirstOrDefaultAsync(q => q.EncryptedToken == request.QRToken);

            if (qrToken != null)
            {
                var scanLog = new QRScanLog
                {
                    QRVerificationTokenId = qrToken.Id,
                    ScanTime = DateTime.UtcNow,
                    IpAddress = device.LastIpAddress,
                    UserAgent = $"DocAttestation App v{device.AppVersion} ({device.Platform})",
                    IsValid = true,
                    ValidationResult = $"Verified by {device.User.UserName} using device {device.DeviceName}"
                };
                _context.QRScanLogs.Add(scanLog);
            }

            await _context.SaveChangesAsync();

            // Get applicant photo as base64
            string? photoBase64 = null;
            if (!string.IsNullOrEmpty(application.ApplicantProfile.PhotographPath))
            {
                var photoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", 
                    application.ApplicantProfile.PhotographPath.TrimStart('/'));
                if (File.Exists(photoPath))
                {
                    var photoBytes = await File.ReadAllBytesAsync(photoPath);
                    photoBase64 = Convert.ToBase64String(photoBytes);
                }
            }

            _logger.LogInformation("QR verified by device {DeviceName} for application {AppNumber}", 
                device.DeviceName, application.ApplicationNumber);

            return new SecureVerificationResult
            {
                IsValid = true,
                Message = "Document verified successfully",
                ApplicantName = application.ApplicantProfile.FullName,
                DocumentType = application.DocumentType,
                IssuingAuthority = application.IssuingAuthority,
                AttestationDate = application.AttestedAt,
                ApplicationNumber = application.ApplicationNumber,
                ApplicantPhoto = photoBase64,
                VerifiedBy = device.User.UserName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying QR from app");
            return new SecureVerificationResult
            {
                IsValid = false,
                Message = "Verification error occurred"
            };
        }
    }

    public async Task<bool> RevokeDeviceAsync(int deviceId, string reason, string adminUserId)
    {
        var device = await _context.RegisteredDevices.FindAsync(deviceId);
        if (device == null) return false;

        device.IsRevoked = true;
        device.RevocationReason = reason;
        device.IsActive = false;

        await _context.SaveChangesAsync();

        _logger.LogWarning("Device {DeviceId} revoked by {AdminId}. Reason: {Reason}", 
            deviceId, adminUserId, reason);

        return true;
    }

    public async Task<List<RegisteredDevice>> GetUserDevicesAsync(string userId)
    {
        return await _context.RegisteredDevices
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.RegisteredAt)
            .ToListAsync();
    }

    public string GenerateSignature(string data, string timestamp, string nonce)
    {
        var message = $"{data}:{timestamp}:{nonce}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.AppSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        return Convert.ToBase64String(hash);
    }

    public bool ValidateSignature(string data, string signature, string timestamp, string nonce)
    {
        var expectedSignature = GenerateSignature(data, timestamp, nonce);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(signature));
    }

    private bool IsVersionValid(string appVersion)
    {
        if (Version.TryParse(appVersion, out var current) && 
            Version.TryParse(_settings.MinimumAppVersion, out var minimum))
        {
            return current >= minimum;
        }
        return false;
    }

    private bool ValidateAppSignature(string appSignature, string deviceId)
    {
        // Validate that the app is genuine using HMAC signature
        // The app generates: HMAC(AppIdentifier + DeviceId, AppSecret)
        var expectedData = $"{_settings.AppIdentifier}:{deviceId}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.AppSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(expectedData));
        var expectedSignature = Convert.ToBase64String(hash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(appSignature));
    }

    private string GenerateSecureToken()
    {
        var bytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }

    private string ComputeHash(string input)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

