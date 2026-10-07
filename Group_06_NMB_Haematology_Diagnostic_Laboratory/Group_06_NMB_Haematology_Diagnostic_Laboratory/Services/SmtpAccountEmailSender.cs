using System.Net;
using System.Net.Mail;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;

public interface IAccountEmailSender
{
    Task SendResetLink(string recipient, string link);
    Task SendTemporaryPassword(string recipient, string name, string password);
}

public class SmtpAccountEmailSender(IConfiguration configuration) : IAccountEmailSender
{
    public Task SendResetLink(string recipient, string link) =>
        Send(recipient, "Reset your NMB-HLabSys password",
            $"A password reset was requested for your account.\n\nOpen this link to choose a new password:\n{link}\n\nThis link expires in 30 minutes and can only be used once.\nIf you did not request this, you can ignore this email.");

    public Task SendTemporaryPassword(string recipient, string name, string password)
    {
        var baseUrl = configuration["AccountSecurity:PublicBaseUrl"];
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || !string.IsNullOrEmpty(uri.UserInfo))
            throw new InvalidOperationException("Configure the trusted HTTPS application URL.");
        return Send(recipient, "Your NMB-HLabSys account",
            $"Hello {name},\n\nYour laboratory account has been created.\n\nUsername: {recipient}\nTemporary password: {password}\n\nSign in: {baseUrl!.TrimEnd('/')}/Account/Login\n\nYou must change this temporary password at your first login. Keep it private.\nIf you need a new password, use Forgot password on the sign-in page.");
    }

    private async Task Send(string recipient, string subject, string body)
    {
        var section = configuration.GetSection("Email");
        var host = section["Host"];
        var from = section["From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("Email:Host and Email:From must be configured.");
        using var client = new SmtpClient(host, section.GetValue("Port", 587))
        {
            EnableSsl = true, UseDefaultCredentials = false, Timeout = 15000
        };
        if (!string.IsNullOrWhiteSpace(section["Username"]))
            client.Credentials = new NetworkCredential(section["Username"], section["Password"]);
        using var message = new MailMessage(from, recipient)
        {
            Subject = subject, Body = body, IsBodyHtml = false
        };
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        await client.SendMailAsync(message, timeout.Token);
    }
}
