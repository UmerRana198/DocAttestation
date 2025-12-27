# Email Service Troubleshooting Guide

## Common Error Messages and Solutions

### 1. "Gmail authentication failed"
**Cause:** Gmail credentials are incorrect or app password has expired.

**Solutions:**
- Verify the email address in `appsettings.json` is correct: `umerrana931909@gmail.com`
- Generate a new Gmail App Password:
  1. Go to your Google Account settings
  2. Enable 2-Step Verification (required for app passwords)
  3. Go to Security → 2-Step Verification → App passwords
  4. Generate a new app password for "Mail"
  5. Update `SenderPassword` in `appsettings.json` with the new 16-character app password
- Make sure you're using an **App Password**, not your regular Gmail password
- App passwords are 16 characters (no spaces)

### 2. "Cannot connect to Gmail SMTP server"
**Cause:** Network or firewall blocking SMTP connection.

**Solutions:**
- Check if the server can reach `smtp.gmail.com`:
  ```powershell
  Test-NetConnection -ComputerName smtp.gmail.com -Port 587
  ```
- Verify port 587 is not blocked by firewall
- Check if your ISP or network blocks SMTP ports
- Try using port 465 instead (requires SSL):
  ```json
  "SmtpPort": 465
  ```

### 3. "Gmail sending limit exceeded"
**Cause:** Gmail daily sending quota reached.

**Solutions:**
- Gmail free accounts: 500 emails/day
- Wait 24 hours for quota to reset
- Consider upgrading to Google Workspace for higher limits
- Check Gmail account for any restrictions

### 4. "Email service is not properly configured"
**Cause:** Missing or empty configuration values.

**Solutions:**
- Verify all EmailSettings in `appsettings.json`:
  - `SmtpServer`: Should be `smtp.gmail.com`
  - `SmtpPort`: Should be `587` (or `465` for SSL)
  - `SenderEmail`: Your Gmail address
  - `SenderPassword`: 16-character app password
  - `EnableSsl`: Should be `true`

## Testing Email Service

### Method 1: Use TestEmail Endpoint
1. Navigate to `/Profile/TestEmail` (if available)
2. Enter your email address
3. Check the response and server logs

### Method 2: Check Application Logs
Look for detailed error messages in:
- `logs/app-YYYYMMDD.log`
- Check for lines containing "SMTP error" or "Email"

### Method 3: Test SMTP Connection Manually
```powershell
# Test connection to Gmail SMTP
$smtp = New-Object System.Net.Mail.SmtpClient("smtp.gmail.com", 587)
$smtp.EnableSsl = $true
$smtp.Credentials = New-Object System.Net.NetworkCredential("your-email@gmail.com", "your-app-password")
try {
    $smtp.Send("from@email.com", "to@email.com", "Test", "Test message")
    Write-Host "Email sent successfully"
} catch {
    Write-Host "Error: $($_.Exception.Message)"
}
```

## Current Configuration

Check `appsettings.json`:
```json
"EmailSettings": {
  "SmtpServer": "smtp.gmail.com",
  "SmtpPort": 587,
  "SenderEmail": "umerrana931909@gmail.com",
  "SenderName": "The Peanut AI",
  "ReplyToEmail": "noreply@thepeanutai.com",
  "SenderPassword": "bmphxtftxtvqqebd",
  "EnableSsl": true,
  "HideSenderEmail": true
}
```

## Quick Fixes

### Fix 1: Regenerate Gmail App Password
1. Go to: https://myaccount.google.com/apppasswords
2. Generate new password for "Mail"
3. Update `appsettings.json`
4. Restart the application

### Fix 2: Verify 2FA is Enabled
- Gmail requires 2-Step Verification for app passwords
- Go to: https://myaccount.google.com/security
- Enable 2-Step Verification if not already enabled

### Fix 3: Check Gmail Account Status
- Make sure account is not locked or restricted
- Check for any security alerts in Gmail
- Verify account can send emails normally

### Fix 4: Try Alternative Port
If port 587 doesn't work, try port 465:
```json
"SmtpPort": 465
```
Note: Port 465 uses SSL/TLS directly (not StartTLS)

## Debugging Steps

1. **Check Logs**: Look at `logs/app-*.log` for detailed error messages
2. **Test Connection**: Use PowerShell to test SMTP connection
3. **Verify Credentials**: Double-check email and app password
4. **Check Network**: Ensure server can reach smtp.gmail.com
5. **Review Error Message**: The new error messages will tell you exactly what's wrong

## Error Message Reference

- `EMAIL_AUTH_ERROR:` → Authentication/credentials issue
- `EMAIL_CONNECTION_ERROR:` → Network/connection issue  
- `EMAIL_QUOTA_ERROR:` → Gmail sending limit exceeded
- `EMAIL_SMTP_ERROR:` → General SMTP error (check logs for details)
- `EMAIL_ERROR:` → General email error

## Still Having Issues?

1. Check the application logs for the exact error message
2. The error message will now be more specific about what's wrong
3. Use the TestEmail endpoint to test email functionality
4. Verify Gmail account settings and app password

