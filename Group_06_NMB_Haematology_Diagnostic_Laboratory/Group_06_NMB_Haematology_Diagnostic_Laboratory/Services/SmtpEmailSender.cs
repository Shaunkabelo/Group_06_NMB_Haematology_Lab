using System.Net;
using System.Net.Mail;
using System.Text;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;

public sealed class SmtpEmailSender(IConfiguration configuration)
{
    public async Task SendAsync(string recipient, string subject, string body)
    {
        var host = configuration["Email:Smtp:Host"];
        var from = configuration["Email:Smtp:FromAddress"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("Configure Email:Smtp:Host and Email:Smtp:FromAddress before sending email notifications.");
        using var message = new MailMessage
        {
            From = new MailAddress(from, configuration["Email:Smtp:FromName"] ?? "NMB-HLabSys"),
            Subject = subject,
            Body = body,
            IsBodyHtml = false,
            BodyEncoding = Encoding.UTF8,
            SubjectEncoding = Encoding.UTF8
        };
        message.To.Add(new MailAddress(recipient));
        using var smtp = new SmtpClient(host, configuration.GetValue("Email:Smtp:Port", 587))
        {
            EnableSsl = configuration.GetValue("Email:Smtp:EnableSsl", true),
            UseDefaultCredentials = false
        };
        var username = configuration["Email:Smtp:Username"];
        if (!string.IsNullOrWhiteSpace(username))
            smtp.Credentials = new NetworkCredential(username, configuration["Email:Smtp:Password"]);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await smtp.SendMailAsync(message, timeout.Token);
    }
}
