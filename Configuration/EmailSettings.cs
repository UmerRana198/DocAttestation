namespace DocAttestation.Configuration;

public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SenderEmail { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string ReplyToEmail { get; set; } = string.Empty;
    public string SenderPassword { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
    public bool HideSenderEmail { get; set; } = false;
}

public class AdminSettings
{
    public List<string> AdminEmails { get; set; } = new();
}

