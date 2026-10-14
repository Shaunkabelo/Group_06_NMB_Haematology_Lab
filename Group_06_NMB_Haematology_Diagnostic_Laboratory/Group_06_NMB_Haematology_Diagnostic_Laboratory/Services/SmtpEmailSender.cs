using System.Net;
using System.Net.Mail;
using System.Text;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;

public sealed class SmtpEmailSender(IConfiguration configuration)
{
    public async Task SendAsync(string recipient, string subject, string body)
    {
        // Account reset emails already use the flat Email:* settings. Prefer the
        // patient-specific nested settings when supplied, then use the same SMTP
        // account as password-reset emails for a consistent deployed setup.
        var host = configuration["Email:Smtp:Host"] ?? configuration["Email:Host"];
        var from = configuration["Email:Smtp:FromAddress"] ?? configuration["Email:From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("Configure Email:Host and Email:From before sending email notifications.");
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
        var port = configuration.GetValue<int?>("Email:Smtp:Port")
            ?? configuration.GetValue("Email:Port", 587);
        using var smtp = new SmtpClient(host, port)
        {
            EnableSsl = configuration.GetValue<bool?>("Email:Smtp:EnableSsl") ?? true,
            UseDefaultCredentials = false
        };
        var username = configuration["Email:Smtp:Username"] ?? configuration["Email:Username"];
        if (!string.IsNullOrWhiteSpace(username))
            smtp.Credentials = new NetworkCredential(username,
                configuration["Email:Smtp:Password"] ?? configuration["Email:Password"]);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        await smtp.SendMailAsync(message, timeout.Token);
    }
}
