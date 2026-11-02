using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;

// Queue every valid email so SMTP/account lookup timing cannot disclose account existence.
public class PasswordResetQueue
{
    private readonly Channel<string> requests = Channel.CreateBounded<string>(100);
    public bool Enqueue(string email) => requests.Writer.TryWrite(email);
    public IAsyncEnumerable<string> ReadAll(CancellationToken cancellationToken) =>
        requests.Reader.ReadAllAsync(cancellationToken);
}

public class PasswordResetWorker(PasswordResetQueue queue, IServiceScopeFactory scopes,
    IConfiguration configuration, ILogger<PasswordResetWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var email in queue.ReadAll(stoppingToken))
        {
            try
            {
                using var scope = scopes.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IAuthenticationRepository>();
                var user = await repository.GetActiveUserByEmail(email);
                if (user == null) continue;
                var baseUrl = configuration["AccountSecurity:PublicBaseUrl"];
                if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var origin) || origin.Scheme != "https" ||
                    !string.IsNullOrEmpty(origin.Query) || !string.IsNullOrEmpty(origin.Fragment) ||
                    !string.IsNullOrEmpty(origin.UserInfo))
                    throw new InvalidOperationException("Configure AccountSecurity:PublicBaseUrl with the application's HTTPS URL.");
                var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
                var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
                if (await repository.CreateReset(user.UserId, hash))
                    await scope.ServiceProvider.GetRequiredService<IAccountEmailSender>()
                        .SendResetLink(user.Email, baseUrl!.TrimEnd('/') + "/Account/ResetPassword?token=" + token);
            }
            catch (Exception exception)
            {
                // Keep reset tokens out of logs, but retain SMTP/configuration details for hosted troubleshooting.
                logger.LogError(exception, "Password reset delivery failed. Check database and email configuration.");
            }
        }
    }
}
