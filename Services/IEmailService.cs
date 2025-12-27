using DocAttestation.Models;

namespace DocAttestation.Services;

public interface IEmailService
{
    Task<bool> SendPaymentConfirmationEmailAsync(Application application, Payment payment);
    Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
}

