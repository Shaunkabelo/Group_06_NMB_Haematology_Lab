using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Text;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;

public interface IConsentEmailService
{
    Task SendGrantedAccessAsync(IReadOnlyList<ConsentEmailDetail> results);
    Task SendRevokedAccessAsync(IReadOnlyList<ConsentEmailDetail> results, DateTime revokedAt);
}

public sealed class ConsentEmailService(IConfiguration configuration) : IConsentEmailService
{
    public static string CreateBody(IReadOnlyList<ConsentEmailDetail> results, DateTime? revokedAt = null)
    {
        if (results.Count == 0) throw new ArgumentException("No saved consent details were supplied.", nameof(results));
        var first = results[0];
        if (results.Any(result => !string.Equals(result.DoctorEmail, first.DoctorEmail, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("A consent notification must target only one doctor.", nameof(results));
        var body = new StringBuilder();
        body.AppendLine($"Dear Dr. {first.DoctorName},");
        body.AppendLine();
        body.AppendLine(revokedAt.HasValue
            ? $"{first.PatientName} has revoked your access to the following test results in NMB-HLabSys:"
            : $"{first.PatientName} has granted you access to view the following test results in NMB-HLabSys:");
        body.AppendLine();
        foreach (var request in results.GroupBy(result => result.RequestNumber).OrderBy(group => group.Key))
        {
            body.AppendLine($"Test request number: {request.Key}");
            foreach (var result in request.OrderBy(result => result.TestName))
                body.AppendLine($"- {result.TestName} | Access {(revokedAt.HasValue ? "revoked" : "granted")}: {(revokedAt ?? result.GrantedDate).ToString("dd MMMM yyyy", CultureInfo.InvariantCulture)}");
            body.AppendLine();
        }
        body.AppendLine(revokedAt.HasValue
            ? "The patient's consent no longer grants you access to these results. Access to other shared results is unchanged."
            : "Sign in to NMB-HLabSys using your doctor account to view these results.");
        body.AppendLine();
        body.AppendLine("NMB-HLabSys | Haematology Laboratory");
        return body.ToString();
    }

    public Task SendGrantedAccessAsync(IReadOnlyList<ConsentEmailDetail> results) => SendAsync(results, null);

    public Task SendRevokedAccessAsync(IReadOnlyList<ConsentEmailDetail> results, DateTime revokedAt) => SendAsync(results, revokedAt);

    private async Task SendAsync(IReadOnlyList<ConsentEmailDetail> results, DateTime? revokedAt)
    {
        var body = CreateBody(results, revokedAt);
        await new SmtpEmailSender(configuration).SendAsync(results[0].DoctorEmail,
            $"NMB-HLabSys - Patient {(revokedAt.HasValue ? "revoked" : "granted")} access to test results", body);
    }
}