using System.Net;
using System.Net.Mail;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;

public interface IAccountEmailSender
{
    Task SendResetLink(string recipient, string link);
}

public class SmtpAccountEmailSender(IConfiguration configuration) : IAccountEmailSender
{
    public async Task SendResetLink(string recipient, string link)
    {
        var section = configuration.GetSection("Email");
        var host = section["Host"];
        var from = section["From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("Email:Host and Email:From must be configured.");
        using var client = new SmtpClient(host, section.GetValue("Port", 587))
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Timeout = 15000
        };
        if (!string.IsNullOrWhiteSpace(section["Username"]))
            client.Credentials = new NetworkCredential(section["Username"], section["Password"]);
        using var message = new MailMessage(from, recipient)
        {
            Subject = "Reset your NMB-HLabSys password",
            Body = $"A password reset was requested for your account.\n\nOpen this link to choose a new password:\n{link}\n\nThis link expires in 30 minutes and can only be used once.\nIf you did not request this, you can ignore this email.",
            IsBodyHtml = false
        };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await client.SendMailAsync(message, timeout.Token);
    }
}
