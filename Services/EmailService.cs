using DocAttestation.Configuration;
using DocAttestation.Models;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace DocAttestation.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
    {
        _emailSettings = emailSettings.Value;
        _logger = logger;
    }

    public async Task<bool> SendPaymentConfirmationEmailAsync(Application application, Payment payment)
    {
        try
        {
            _logger.LogInformation("Attempting to send payment confirmation email for application {ApplicationId}", application.Id);
            
            // Try to get email from User first, then fallback to ApplicantProfile.Email
            var userEmail = application.ApplicantProfile?.User?.Email;
            var profileEmail = application.ApplicantProfile?.Email;
            var applicantEmail = userEmail ?? profileEmail;
            
            _logger.LogInformation("Email lookup - User Email: {UserEmail}, Profile Email: {ProfileEmail}, Final Email: {FinalEmail}", 
                userEmail ?? "null", profileEmail ?? "null", applicantEmail ?? "null");
            
            if (string.IsNullOrEmpty(applicantEmail))
            {
                _logger.LogWarning("Cannot send email: Applicant email is not available for application {ApplicationId}. User: {UserEmail}, Profile: {ProfileEmail}", 
                    application.Id, userEmail ?? "null", profileEmail ?? "null");
                return false;
            }

            var emailBody = GeneratePaymentConfirmationEmailBody(application, payment);
            var subject = $"Payment Confirmation - Application {application.ApplicationNumber}";

            _logger.LogInformation("Sending email to {Email} with subject: {Subject}", applicantEmail, subject);
            var result = await SendEmailAsync(applicantEmail, subject, emailBody, true);
            
            if (result)
            {
                _logger.LogInformation("Payment confirmation email sent successfully to {Email} for application {ApplicationId}", applicantEmail, application.Id);
            }
            else
            {
                _logger.LogWarning("Failed to send payment confirmation email to {Email} for application {ApplicationId}", applicantEmail, application.Id);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payment confirmation email for application {ApplicationId}: {ErrorMessage}", application.Id, ex.Message);
            return false;
        }
    }

    public async Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        try
        {
            // Validate email settings
            if (string.IsNullOrEmpty(_emailSettings.SmtpServer))
            {
                _logger.LogError("SMTP Server is not configured");
                throw new InvalidOperationException("SMTP Server is not configured");
            }
            
            if (string.IsNullOrEmpty(_emailSettings.SenderEmail) || string.IsNullOrEmpty(_emailSettings.SenderPassword))
            {
                _logger.LogError("Email credentials are not configured");
                throw new InvalidOperationException("Email credentials are not configured");
            }
            
            _logger.LogInformation("Configuring SMTP client - Server: {Server}, Port: {Port}, From: {From}", 
                _emailSettings.SmtpServer, _emailSettings.SmtpPort, _emailSettings.SenderEmail);
            
            // Gmail: Port 587 uses StartTLS (EnableSsl = true), Port 465 uses SSL/TLS
            var enableSsl = _emailSettings.SmtpPort == 587 || _emailSettings.SmtpPort == 465 || _emailSettings.EnableSsl;
            
            using var client = new SmtpClient(_emailSettings.SmtpServer, _emailSettings.SmtpPort)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(_emailSettings.SenderEmail, _emailSettings.SenderPassword),
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = 60000, // 60 seconds timeout
                UseDefaultCredentials = false
            };

            using var message = new MailMessage
            {
                From = new MailAddress(_emailSettings.SenderEmail, _emailSettings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = isHtml
            };

            if (!string.IsNullOrEmpty(_emailSettings.ReplyToEmail))
            {
                message.ReplyToList.Add(new MailAddress(_emailSettings.ReplyToEmail));
            }

            message.To.Add(to);

            _logger.LogInformation("Attempting to send email via SMTP to {To}", to);
            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent successfully to {Email}", to);
            return true;
        }
        catch (SmtpException smtpEx)
        {
            var errorDetails = $"SMTP Error - StatusCode: {smtpEx.StatusCode}, Message: {smtpEx.Message}";
            if (smtpEx.InnerException != null)
            {
                errorDetails += $", InnerException: {smtpEx.InnerException.Message}";
            }
            _logger.LogError(smtpEx, "SMTP error sending email to {Email}: {ErrorDetails}", to, errorDetails);
            
            // Determine specific error type and throw with detailed message
            string errorMessage;
            if (smtpEx.Message.Contains("authentication") || smtpEx.Message.Contains("credential") || 
                smtpEx.Message.Contains("535") || smtpEx.StatusCode == SmtpStatusCode.MustIssueStartTlsFirst)
            {
                errorMessage = "EMAIL_AUTH_ERROR: Gmail authentication failed. Please verify: 1) Email address is correct, 2) App Password is correct (not regular password), 3) 2FA is enabled on Gmail account, 4) App Password hasn't expired";
                _logger.LogError("Authentication failed. Please verify: 1) Email address is correct, 2) App Password is correct (not regular password), 3) 2FA is enabled on Gmail account");
            }
            else if (smtpEx.Message.Contains("timeout") || smtpEx.Message.Contains("connection") || 
                     smtpEx.StatusCode == SmtpStatusCode.ServiceNotAvailable)
            {
                errorMessage = "EMAIL_CONNECTION_ERROR: Cannot connect to Gmail SMTP server. Please verify: 1) Server can reach smtp.gmail.com, 2) Port 587 is not blocked by firewall, 3) Internet connection is working";
                _logger.LogError("Connection failed. Please verify: 1) Server can reach smtp.gmail.com, 2) Port 587 is not blocked by firewall");
            }
            else if (smtpEx.Message.Contains("quota") || smtpEx.Message.Contains("limit"))
            {
                errorMessage = "EMAIL_QUOTA_ERROR: Gmail sending limit exceeded. Please try again later or check Gmail account limits";
            }
            else
            {
                errorMessage = $"EMAIL_SMTP_ERROR: {smtpEx.Message}";
            }
            
            throw new InvalidOperationException(errorMessage, smtpEx);
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            var fullError = $"Error: {ex.Message}";
            if (ex.InnerException != null)
            {
                fullError += $", InnerException: {ex.InnerException.Message}";
            }
            _logger.LogError(ex, "Error sending email to {Email}: {FullError} - StackTrace: {StackTrace}", to, fullError, ex.StackTrace);
            throw new InvalidOperationException($"EMAIL_ERROR: {ex.Message}", ex);
        }
    }

    private string GetSubmittedByRow(Application application, string submittedBy)
    {
        if (!application.SubmissionBy.HasValue)
            return "";
        
        return $@"
            <tr>
                <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0;""><strong>Submitted By:</strong></td>
                <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0;"">{submittedBy}</td>
            </tr>";
    }

    private string GeneratePaymentConfirmationEmailBody(Application application, Payment payment)
    {
        var profile = application.ApplicantProfile;
        
        // Build submission method text based on selected option
        string submissionMethod;
        if (application.DocumentSubmissionMethod == DocumentSubmissionMethod.Physical)
        {
            if (application.SubmissionBy == SubmissionBy.ByYourself)
            {
                submissionMethod = "Physical - By Yourself";
            }
            else if (application.SubmissionBy == SubmissionBy.BloodRelation)
            {
                submissionMethod = "Physical - Blood Relation";
            }
            else
            {
                submissionMethod = "Physical";
            }
        }
        else
        {
            submissionMethod = "TCS (Courier Service)";
        }
        
        var submittedBy = application.SubmissionBy == SubmissionBy.ByYourself ? "By Yourself" : 
                         application.SubmissionBy == SubmissionBy.BloodRelation ? $"Blood Relation ({application.RelationType ?? "N/A"})" : "N/A";
        
        var tcsInfo = "";
        if (application.DocumentSubmissionMethod == DocumentSubmissionMethod.TCS && !string.IsNullOrEmpty(application.TCSNumber))
        {
            tcsInfo = $@"
                <tr>
                    <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0;""><strong>TCS Tracking Number:</strong></td>
                    <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0;"">{application.TCSNumber}</td>
                </tr>
                <tr>
                    <td colspan=""2"" style=""padding: 10px; color: #666; font-size: 12px;"">
                        Please use this tracking number to track your document shipment via TCS.
                    </td>
                </tr>";
        }

        var timeSlotInfo = "";
        if (application.TimeSlot.HasValue)
        {
            var timeSlot = application.TimeSlot.Value;
            timeSlotInfo = $@"
                <tr>
                    <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0;""><strong>Assigned Time Slot:</strong></td>
                    <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0;"">{timeSlot.ToString("dd MMM yyyy 'at' hh:mm tt")}</td>
                </tr>";
        }

        var documentSubmissionInstructions = "";
        if (application.DocumentSubmissionMethod == DocumentSubmissionMethod.Physical && application.TimeSlot.HasValue)
        {
            var bloodRelationInstructions = "";
            if (application.SubmissionBy == SubmissionBy.BloodRelation)
            {
                bloodRelationInstructions = $@"
                    <div style=""background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 15px 0; border-radius: 4px;"">
                        <h4 style=""margin: 0 0 10px 0; color: #856404;"">⚠️ Important: Blood Relation Submission</h4>
                        <p style=""margin: 5px 0; color: #856404;"">
                            <strong>The blood relation person ({application.RelationType ?? "N/A"}) must bring their <u>ORIGINAL CNIC</u> when submitting documents.</strong><br/>
                            <strong>CNIC Number Required:</strong> {application.RelationCNIC ?? "N/A"}
                        </p>
                        <p style=""margin: 5px 0; color: #856404;"">
                            Without the original CNIC of the blood relation, documents will not be accepted.
                        </p>
                    </div>";
            }
            
            documentSubmissionInstructions = $@"
                <div style=""background-color: #e8f5e9; border-left: 4px solid #4caf50; padding: 15px; margin: 20px 0; border-radius: 4px;"">
                    <h3 style=""margin: 0 0 10px 0; color: #2e7d32;"">📅 Document Submission Instructions</h3>
                    <p style=""margin: 5px 0; color: #1b5e20;"">
                        <strong>Please submit your original documents physically on the assigned time slot:</strong><br/>
                        <strong>Date & Time:</strong> {application.TimeSlot.Value.ToString("dd MMM yyyy 'at' hh:mm tt")}
                    </p>
                    <p style=""margin: 5px 0; color: #1b5e20;"">
                        Please bring all original documents along with a copy of this payment receipt.
                    </p>
                    {bloodRelationInstructions}
                </div>";
        }
        else if (application.DocumentSubmissionMethod == DocumentSubmissionMethod.TCS)
        {
            documentSubmissionInstructions = $@"
                <div style=""background-color: #e3f2fd; border-left: 4px solid #2196f3; padding: 15px; margin: 20px 0; border-radius: 4px;"">
                    <h3 style=""margin: 0 0 10px 0; color: #1565c0;"">📦 Document Submission via TCS</h3>
                    <p style=""margin: 5px 0; color: #0d47a1;"">
                        Please send your original documents via TCS courier service using the tracking number provided above.
                    </p>
                    <p style=""margin: 5px 0; color: #0d47a1;"">
                        Make sure to include a copy of this payment receipt with your documents.
                    </p>
                </div>";
        }

        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Payment Confirmation</title>
</head>
<body style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f5f5f5;"">
    <div style=""background-color: #ffffff; border-radius: 8px; padding: 30px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);"">
        <!-- Header -->
        <div style=""text-align: center; border-bottom: 3px solid #006633; padding-bottom: 20px; margin-bottom: 30px;"">
            <h1 style=""color: #006633; margin: 0; font-size: 28px;"">✅ Payment Confirmation</h1>
            <p style=""color: #666; margin: 10px 0 0 0;"">Document Attestation Application</p>
        </div>

        <!-- Success Message -->
        <div style=""background-color: #e8f5e9; border-left: 4px solid #4caf50; padding: 15px; margin-bottom: 30px; border-radius: 4px;"">
            <p style=""margin: 0; color: #2e7d32; font-size: 16px;"">
                <strong>Thank you for your payment!</strong> Your application has been successfully submitted and is now under review.
            </p>
        </div>

        <!-- Application Details -->
        <h2 style=""color: #006633; font-size: 20px; margin-top: 30px; margin-bottom: 15px; border-bottom: 2px solid #e0e0e0; padding-bottom: 10px;"">Application Information</h2>
        <table style=""width: 100%; border-collapse: collapse; margin-bottom: 30px;"">
            <tr>
                <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0;""><strong>Application Number:</strong></td>
                <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0; color: #006633; font-weight: bold;"">{application.ApplicationNumber}</td>
            </tr>
            <tr>
                <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0;""><strong>Applicant Name:</strong></td>
                <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0;"">{profile?.FullName ?? "N/A"}</td>
            </tr>
            <tr>
                <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0;""><strong>Document Type:</strong></td>
                <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0;"">{application.DocumentType}</td>
            </tr>
            <tr>
                <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0;""><strong>Issuing Authority:</strong></td>
                <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0;"">{application.IssuingAuthority}</td>
            </tr>
            <tr>
                <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0;""><strong>Verification Type:</strong></td>
                <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0;"">{(application.VerificationType == VerificationType.Normal ? "Normal" : "Urgent")}</td>
            </tr>
            <tr>
                <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0;""><strong>Document Submission Method:</strong></td>
                <td style=""padding: 10px; border-bottom: 1px solid #e0e0e0;"">{submissionMethod}</td>
            </tr>
            {GetSubmittedByRow(application, submittedBy)}
            {tcsInfo}
            {timeSlotInfo}
        </table>

        <!-- Payment Receipt -->
        <h2 style=""color: #006633; font-size: 20px; margin-top: 30px; margin-bottom: 15px; border-bottom: 2px solid #e0e0e0; padding-bottom: 10px;"">Payment Receipt</h2>
        <table style=""width: 100%; border-collapse: collapse; margin-bottom: 30px; background-color: #f9f9f9; border-radius: 4px; overflow: hidden;"">
            <tr>
                <td style=""padding: 15px; border-bottom: 1px solid #e0e0e0;""><strong>Payment ID:</strong></td>
                <td style=""padding: 15px; border-bottom: 1px solid #e0e0e0;"">#{payment.Id}</td>
            </tr>
            <tr>
                <td style=""padding: 15px; border-bottom: 1px solid #e0e0e0;""><strong>Amount Paid:</strong></td>
                <td style=""padding: 15px; border-bottom: 1px solid #e0e0e0; color: #4caf50; font-size: 18px; font-weight: bold;"">PKR {payment.Amount.ToString("N2")}</td>
            </tr>
            <tr>
                <td style=""padding: 15px; border-bottom: 1px solid #e0e0e0;""><strong>Payment Method:</strong></td>
                <td style=""padding: 15px; border-bottom: 1px solid #e0e0e0;"">{payment.PaymentMethod}</td>
            </tr>
            <tr>
                <td style=""padding: 15px; border-bottom: 1px solid #e0e0e0;""><strong>Card Number (Last 4 digits):</strong></td>
                <td style=""padding: 15px; border-bottom: 1px solid #e0e0e0;"">**** **** **** {payment.CardNumber}</td>
            </tr>
            <tr>
                <td style=""padding: 15px;""><strong>Payment Date:</strong></td>
                <td style=""padding: 15px;"">{payment.PaidAt.ToString("dd MMM yyyy HH:mm")} UTC</td>
            </tr>
        </table>

        {documentSubmissionInstructions}

        <!-- Next Steps -->
        <div style=""background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; border-radius: 4px;"">
            <h3 style=""margin: 0 0 10px 0; color: #856404;"">📋 Next Steps</h3>
            <ul style=""margin: 5px 0; padding-left: 20px; color: #856404;"">
                <li>Keep this email as your payment receipt</li>
                <li>Your application is now under review by our verification team</li>
                <li>You will be notified via email about the status of your application</li>
                <li>Please ensure you submit your original documents as per the instructions above</li>
            </ul>
        </div>

        <!-- Footer -->
        <div style=""margin-top: 40px; padding-top: 20px; border-top: 2px solid #e0e0e0; text-align: center; color: #666; font-size: 12px;"">
            <p style=""margin: 5px 0;"">This is an automated email. Please do not reply to this message.</p>
            <p style=""margin: 5px 0;"">If you have any questions, please contact our support team.</p>
            <p style=""margin: 10px 0 0 0; color: #999;"">&copy; {DateTime.UtcNow.Year} Document Attestation System. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
    }
}

