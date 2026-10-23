using System.Net;
using System.Net.Mail;
using System.Diagnostics;
using System.Text.Json;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Microsoft.AspNetCore.DataProtection;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;

public sealed record DoctorEmail(string Recipient, string Subject, string Body,
    int DoctorId = 0, bool AttachResults = false);

public sealed class DoctorEmailProtector(IDataProtectionProvider provider)
{
    private readonly IDataProtector protector = provider.CreateProtector("NMB.DoctorEmailQueue.v1");
    public string Protect(DoctorEmail email) => protector.Protect(JsonSerializer.Serialize(email));
    public DoctorEmail Read(string payload) => JsonSerializer.Deserialize<DoctorEmail>(protector.Unprotect(payload))
        ?? throw new InvalidOperationException("Invalid queued message.");
}

public interface IDoctorEmailSender
{
    Task Send(DoctorEmail email, byte[]? pdf = null, CancellationToken cancellationToken = default);
}

public sealed class DoctorEmailSender(IConfiguration configuration, ILogger<DoctorEmailSender> logger) : IDoctorEmailSender
{
    public async Task Send(DoctorEmail email, byte[]? pdf = null, CancellationToken cancellationToken = default)
    {
        if (email.AttachResults && (pdf is null || pdf.Length == 0))
            throw new InvalidOperationException("A results PDF is required for this patient email.");

        var settings = configuration.GetSection("Email");
        var host = settings["Host"];
        var from = settings["From"];
        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from))
            throw new InvalidOperationException("Configure Email:Host and Email:From.");
        using var client = new SmtpClient(host, settings.GetValue("Port", 587))
        { EnableSsl = true, UseDefaultCredentials = false };
        if (!string.IsNullOrWhiteSpace(settings["Username"]))
            client.Credentials = new NetworkCredential(settings["Username"], settings["Password"]);
        using var message = new MailMessage(from, email.Recipient)
        { Subject = email.Subject, Body = email.Body, IsBodyHtml = false };
        if (email.AttachResults)
            message.Attachments.Add(new Attachment(new MemoryStream(pdf!), "laboratory-results.pdf", "application/pdf"));
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(20));
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await client.SendMailAsync(message, timeout.Token);
            logger.LogInformation("SMTP accepted doctor email in {ElapsedMilliseconds} ms.", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception exception)
        {
            logger.LogWarning("SMTP send failed after {ElapsedMilliseconds} ms ({FailureType}).",
                stopwatch.ElapsedMilliseconds, exception.GetType().Name);
            throw;
        }
    }
}

public sealed class DoctorEmailWorker(IServiceScopeFactory scopes, ILogger<DoctorEmailWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = false;
            try
            {
                using var scope = scopes.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IDoctorWorkflowRepository>();
                var job = await repository.ClaimEmail();
                if (job != null)
                {
                    processed = true;
                    try
                    {
                        DoctorEmail email;
                        byte[]? attachment = null;
                        if (job.Kind == "Cancellation")
                        {
                            var recipient = await repository.CancellationRecipient(job.TestRequestId!.Value)
                                ?? throw new InvalidOperationException("Cancelled request no longer available.");
                            email = new(recipient.Email, $"Test request #{job.TestRequestId} cancelled",
                                $"Hello {recipient.Name},\n\nTest request #{job.TestRequestId} was cancelled.\nReason: {recipient.Reason}");
                        }
                        else
                        {
                            email = scope.ServiceProvider.GetRequiredService<DoctorEmailProtector>().Read(job.Payload!);
                            email = email with { Body = email.Body.Replace("{{RequestID}}", job.TestRequestId?.ToString() ?? "") };
                            if (email.AttachResults)
                            {
                                var request = await repository.Request(email.DoctorId, job.TestRequestId!.Value);
                                if (request == null || request.DoctorId != email.DoctorId || request.Status != "Released by doctor")
                                    throw new InvalidOperationException("Request is not released by this doctor.");
                                var results = (await repository.Results(email.DoctorId, request.Id)).ToList();
                                if (results.Count == 0 || results.Any(r => r.VerificationStatus != "Verified"))
                                    throw new InvalidOperationException("Results are no longer verified.");
                                attachment = DoctorRequestsPdfReport.BuildResults(request, results);
                            }
                        }
                        await scope.ServiceProvider.GetRequiredService<IDoctorEmailSender>().Send(email, attachment, stoppingToken);
                        await repository.CompleteEmail(job, true);
                    }
                    catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
                    {
                        // Do not log addresses, medical details, or decrypted temporary passwords.
                        logger.LogWarning("Doctor email job {JobId} failed on attempt {Attempt} ({FailureType}); it will be retried with backoff.",
                            job.Id, job.Attempts, exception.GetType().Name);
                        await repository.CompleteEmail(job, false);
                    }
                }
            }
            catch (Exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogWarning("Doctor email queue is unavailable. Check database deployment and connectivity.");
            }
            // Keep queue pickup responsive without continuously querying SQL Server.
            if (!processed) await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
        }
    }
}
