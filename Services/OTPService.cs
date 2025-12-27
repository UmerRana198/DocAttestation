using DocAttestation.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DocAttestation.Services;

public class OTPService : IOTPService
{
    private readonly IMemoryCache _cache;
    private readonly IEmailService _emailService;
    private readonly ILogger<OTPService> _logger;
    private const int OTP_EXPIRATION_MINUTES = 10;
    private const int OTP_LENGTH = 6;

    public OTPService(
        IMemoryCache cache,
        IEmailService emailService,
        ILogger<OTPService> logger)
    {
        _cache = cache;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<string> GenerateAndSendOTPAsync(string email)
    {
        string? otp = null;
        string? cacheKey = null;
        
        try
        {
            // Validate email
            if (string.IsNullOrWhiteSpace(email) || !email.Contains("@"))
            {
                _logger.LogError("Invalid email address provided for OTP: {Email}", email ?? "null");
                throw new ArgumentException("Invalid email address", nameof(email));
            }

            // Generate 6-digit OTP
            var random = new Random();
            otp = random.Next(100000, 999999).ToString();

            // Store OTP in cache with expiration
            cacheKey = $"OTP_{email}";
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(OTP_EXPIRATION_MINUTES),
                SlidingExpiration = TimeSpan.FromMinutes(OTP_EXPIRATION_MINUTES)
            };
            _cache.Set(cacheKey, otp, cacheOptions);

            _logger.LogInformation("OTP generated for email {Email}. Attempting to send email...", email);

            // Send OTP via email
            var emailBody = GenerateOTPEmailBody(otp);
            var subject = "OTP Verification - Document Attestation";
            
            try
            {
                var emailSent = await _emailService.SendEmailAsync(email, subject, emailBody, true);
                
                if (emailSent)
                {
                    _logger.LogInformation("OTP email sent successfully to {Email}", email);
                    return otp; // Return for testing purposes, but in production you might want to return empty
                }
                else
                {
                    // This shouldn't happen now as EmailService throws exceptions, but keep for safety
                    if (!string.IsNullOrEmpty(cacheKey))
                    {
                        _cache.Remove(cacheKey);
                    }
                    throw new InvalidOperationException("EMAIL_ERROR: Email service returned false without throwing exception");
                }
            }
            catch (InvalidOperationException)
            {
                // Re-throw InvalidOperationException from EmailService as-is (it has detailed error messages)
                // Remove OTP from cache if email failed
                if (!string.IsNullOrEmpty(cacheKey))
                {
                    _cache.Remove(cacheKey);
                }
                throw;
            }
        }
        catch (ArgumentException)
        {
            // Re-throw argument exceptions as-is
            throw;
        }
        catch (InvalidOperationException)
        {
            // Re-throw InvalidOperationException as-is (with our custom message)
            throw;
        }
        catch (Exception ex)
        {
            // Remove OTP from cache if it was set
            if (!string.IsNullOrEmpty(cacheKey))
            {
                _cache.Remove(cacheKey);
            }
            
            _logger.LogError(ex, "Unexpected error generating and sending OTP to {Email}: {Message}", email, ex.Message);
            throw new InvalidOperationException($"Failed to send OTP email: {ex.Message}", ex);
        }
    }

    public bool VerifyOTP(string email, string otp)
    {
        try
        {
            var cacheKey = $"OTP_{email}";
            if (_cache.TryGetValue(cacheKey, out string? storedOtp))
            {
                if (storedOtp == otp)
                {
                    // Mark OTP as verified (store verification status)
                    var verifiedKey = $"OTP_VERIFIED_{email}";
                    _cache.Set(verifiedKey, true, TimeSpan.FromMinutes(30)); // Keep verified for 30 minutes
                    _cache.Remove(cacheKey); // Remove OTP after verification
                    _logger.LogInformation("OTP verified successfully for email {Email}", email);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Invalid OTP provided for email {Email}", email);
                    return false;
                }
            }
            else
            {
                _logger.LogWarning("OTP not found or expired for email {Email}", email);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying OTP for email {Email}", email);
            return false;
        }
    }

    public void ClearOTP(string email)
    {
        var cacheKey = $"OTP_{email}";
        var verifiedKey = $"OTP_VERIFIED_{email}";
        _cache.Remove(cacheKey);
        _cache.Remove(verifiedKey);
        _logger.LogInformation("OTP cleared for email {Email}", email);
    }

    public bool IsOTPVerified(string email)
    {
        var verifiedKey = $"OTP_VERIFIED_{email}";
        return _cache.TryGetValue(verifiedKey, out bool verified) && verified;
    }

    private string GenerateOTPEmailBody(string otp)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>OTP Verification</title>
</head>
<body style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f5f5f5;"">
    <div style=""background-color: #ffffff; border-radius: 8px; padding: 30px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"">
        <!-- Header -->
        <div style=""text-align: center; border-bottom: 3px solid #006633; padding-bottom: 20px; margin-bottom: 30px;"">
            <h1 style=""color: #006633; margin: 0; font-size: 28px;"">🔐 OTP Verification</h1>
            <p style=""color: #666; margin: 10px 0 0 0;"">Document Attestation System</p>
        </div>

        <!-- OTP Code -->
        <div style=""background-color: #e8f5e9; border-left: 4px solid #4caf50; padding: 20px; margin-bottom: 30px; border-radius: 4px; text-align: center;"">
            <p style=""margin: 0 0 10px 0; color: #2e7d32; font-size: 14px; font-weight: 600;"">Your One-Time Password (OTP) is:</p>
            <div style=""font-size: 32px; font-weight: bold; color: #006633; letter-spacing: 8px; margin: 15px 0;"">{otp}</div>
            <p style=""margin: 10px 0 0 0; color: #1b5e20; font-size: 12px;"">This OTP is valid for 10 minutes</p>
        </div>

        <!-- Instructions -->
        <div style=""background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 4px;"">
            <h3 style=""margin: 0 0 10px 0; color: #856404;"">📋 Instructions</h3>
            <ul style=""margin: 5px 0; padding-left: 20px; color: #856404;"">
                <li>Enter this OTP in the payment verification form</li>
                <li>Do not share this OTP with anyone</li>
                <li>The OTP will expire in 10 minutes</li>
                <li>If you did not request this OTP, please ignore this email</li>
            </ul>
        </div>

        <!-- Security Notice -->
        <div style=""margin-top: 30px; padding-top: 20px; border-top: 2px solid #e0e0e0; text-align: center; color: #666; font-size: 12px;"">
            <p style=""margin: 5px 0;"">This is an automated email. Please do not reply to this message.</p>
            <p style=""margin: 5px 0; color: #999;"">&copy; {DateTime.UtcNow.Year} Document Attestation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }
}


