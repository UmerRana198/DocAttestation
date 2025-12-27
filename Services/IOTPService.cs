namespace DocAttestation.Services;

public interface IOTPService
{
    Task<string> GenerateAndSendOTPAsync(string email);
    bool VerifyOTP(string email, string otp);
    void ClearOTP(string email);
    bool IsOTPVerified(string email);
}

