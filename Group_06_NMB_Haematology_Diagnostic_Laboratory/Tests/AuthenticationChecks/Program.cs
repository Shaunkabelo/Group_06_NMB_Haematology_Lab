using System.Security.Cryptography;
using System.Text;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Helpers;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers;
using Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    Console.WriteLine("PASS: " + name);
    checks++;
}

const string password = "SecurePass8!";
var salt = PasswordHelper.GenerateSalt();
var hash = PasswordHelper.HashPassword(password, salt);
Check(PasswordHelper.VerifyPassword(password, hash, salt), "PBKDF2 password verifies");
Check(!PasswordHelper.VerifyPassword("wrong", hash, salt), "Wrong password rejected");
var legacy = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password + salt)));
Check(PasswordHelper.VerifyPassword(password, legacy, salt), "Existing SHA-256 password still verifies");
Check(!PasswordHelper.VerifyPassword(password, "placeholder", "placeholder"), "Seed placeholder hash rejected");
Check(!PasswordHelper.VerifyPassword(password, "PBKDF2-SHA256$999999999$AA==", salt), "Unbounded iteration count rejected");
Check(PasswordHelper.MeetsPolicy("Abcdef1!"), "Eight-character required policy accepted");
foreach (var invalid in new[] { "Abcd1!", "abcdef1!", "Abcdefg!", "Abcdef12", "Abcdef1 ", new string('A', 129) + "1!" })
    Check(!PasswordHelper.MeetsPolicy(invalid), "Invalid password policy rejected: " + invalid.Length);
var temporary = Enumerable.Range(0, 50).Select(_ => PasswordHelper.GenerateTemporaryPassword()).ToArray();
Check(temporary.All(PasswordHelper.MeetsPolicy) && temporary.Distinct().Count() == temporary.Length,
    "Generated passwords are unique and satisfy policy");

var repository = new FakeRepository();
AccountController Controller() => new(repository, new PasswordResetQueue(), NullLogger<AccountController>.Instance, new AccountNavigation(
    new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(), new EmptyActions()));
var controller = Controller();
Check(await controller.Login("invalid-email", password) is ViewResult && repository.Lookups == 0,
    "Malformed login rejected before database access");
controller = Controller();
Check(await controller.Login("unknown@example.com", password) is ViewResult && !controller.ModelState.IsValid,
    "Unknown login returns validation error");

controller = Controller();
var unknownResult = controller.ForgotPassword("unknown@example.com") as RedirectToActionResult;
controller = Controller();
var knownResult = controller.ForgotPassword("known@example.com") as RedirectToActionResult;
Check(unknownResult?.ActionName == "ForgotPasswordConfirmation" &&
    unknownResult.ActionName == knownResult?.ActionName, "Reset requests return identical confirmation");
var before = repository.ResetCalls;
controller = Controller();
Check(await controller.ResetPassword("bad", password, password) is ViewResult && repository.ResetCalls == before,
    "Malformed token never reaches database");
controller = Controller();
Check(await controller.ResetPassword(new string('A', 64), password, "Mismatch8!") is ViewResult &&
    repository.ResetCalls == before, "Password mismatch does not consume token");
controller = Controller();
Check(await controller.ResetPassword(new string('A', 64), "weak", "weak") is ViewResult &&
    repository.ResetCalls == before, "Weak password does not consume token");
controller = Controller();
Check(await controller.ResetPassword(new string('A', 64), password, password) is ViewResult &&
    repository.ResetCalls == before + 1 && !controller.ModelState.IsValid,
    "Expired/used token rejected");
Check(repository.LastTokenHash != new string('A', 64) && repository.LastTokenHash?.Length == 64,
    "Repository receives token hash, not raw token");

Console.WriteLine($"{checks} authentication checks passed.");

sealed class FakeRepository : IAuthenticationRepository
{
    public Task<User?> GetActiveUserById(int id) => Task.FromResult<User?>(null);
    public Task<bool> UpdateProfile(int id, Guid stamp, string name, string surname, string email, string? cellphone, string? address) => Task.FromResult(false);
    public int Lookups { get; private set; }
    public int ResetCalls { get; private set; }
    public string? LastTokenHash { get; private set; }
    public Task<User?> GetActiveUserByEmail(string email) { Lookups++; return Task.FromResult<User?>(null); }
    public Task RecordLogin(int id) => Task.CompletedTask;
    public Task<AuthenticationState?> GetState(int id) => Task.FromResult<AuthenticationState?>(null);
    public Task<bool> CreateReset(int id, string hash) => Task.FromResult(true);
    public Task<bool> ResetPassword(string tokenHash, string hash, string salt)
    {
        ResetCalls++;
        LastTokenHash = tokenHash;
        return Task.FromResult(false);
    }
    public Task<bool> ChangePassword(int id, string old, string hash, string salt) => Task.FromResult(false);
}

sealed class EmptyActions : Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider
{
    public Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollection ActionDescriptors { get; } =
        new(Array.Empty<Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor>(), 0);
}