using System.Globalization;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;

public interface IRegistrationEmailService
{
    Task SendAccountCreatedAsync(string email, string name, string surname, DateTime createdUtc);
}

public sealed class RegistrationEmailService(IConfiguration configuration) : IRegistrationEmailService
{
    public static string CreateBody(string email, string name, string surname, DateTime createdUtc) =>
        $"Dear {name.Trim()} {surname.Trim()},\n\n" +
        "Your NMB-HLabSys patient account has been created successfully.\n\n" +
        $"Username: {email.Trim()}\n" +
        $"Account created: {createdUtc.AddHours(2).ToString("dd MMMM yyyy HH:mm", CultureInfo.InvariantCulture)} SAST\n\n" +
        "You can now sign in to NMB-HLabSys using your email address and the password you chose during registration.\n\n" +
        "NMB-HLabSys | Haematology Laboratory";

    public Task SendAccountCreatedAsync(string email, string name, string surname, DateTime createdUtc) =>
        new SmtpEmailSender(configuration).SendAsync(email.Trim(),
            "NMB-HLabSys - Patient account created successfully", CreateBody(email, name, surname, createdUtc));
}
