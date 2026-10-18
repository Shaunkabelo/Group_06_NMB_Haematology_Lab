using System.Security.Cryptography;
using System.Text;
using System.Data;
using Dapper;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Microsoft.Extensions.Configuration;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Helpers;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers;
using Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;

var checks = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception("FAILED: " + name);
    Console.WriteLine("PASS: " + name);
    checks++;
}

// Exercise the same Dapper row mapping used by the login repository without SQL Server.
_ = new SqlDataAccess(new ConfigurationBuilder().Build());
using (var table = new DataTable())
{
    table.Columns.Add("DateOfBirth", typeof(DateTime));
    table.Rows.Add(new DateTime(1990, 1, 1));
    table.Rows.Add(DBNull.Value);
    using var reader = table.CreateDataReader();
    var parse = reader.GetRowParser<User>();
    Check(reader.Read() && parse(reader).DateOfBirth == new DateOnly(1990, 1, 1),
        "Login user maps SQL DateTime birth date to nullable DateOnly");
    Check(reader.Read() && parse(reader).DateOfBirth == null,
        "Login user preserves NULL birth date");
}
var dateParameter = new Microsoft.Data.SqlClient.SqlParameter();
new DateOnlyTypeHandler().SetValue(dateParameter, new DateOnly(1990, 1, 1));
Check(dateParameter.DbType == DbType.Date &&
    dateParameter.Value is DateTime dateValue && dateValue == new DateTime(1990, 1, 1),
    "DateOnly parameters use SQL date values");

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
var actions = new TestActions();
var navigation = new AccountNavigation(new ConfigurationBuilder().Build(), actions);
AccountController Controller() => new(repository, new PasswordResetQueue(), NullLogger<AccountController>.Instance, navigation);
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

var stamp = Guid.NewGuid();
repository.LoginUser = new User { UserId = 1, Name = "Test", Surname = "Doctor",
    Email = "doctor@example.com", PasswordHash = hash, PasswordSalt = salt, SecurityStamp = stamp };
repository.State = new AuthenticationState { SecurityStamp = stamp, Role = "Doctor" };
var authentication = new FakeAuthenticationService();
var serviceCollection = new ServiceCollection().AddLogging().AddSingleton<IAuthenticationService>(authentication);
serviceCollection.AddControllersWithViews();
using var services = serviceCollection.BuildServiceProvider();
AccountController LoginController(bool signedIn = false)
{
    var result = Controller();
    result.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = services } };
    result.Url = new TestUrlHelper();
    if (signedIn)
        result.HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Doctor") }, "Cookies"));
    return result;
}
bool DoctorDashboard(IActionResult result) => result is RedirectToActionResult { ControllerName: "Doctor", ActionName: "Dashboard" };
Check(DoctorDashboard(await LoginController().Login("doctor@example.com", password)) && authentication.SignIns == 1,
    "Successful doctor login issues a session and redirects to doctor dashboard");
Check(LoginController(true).Login() is RedirectToActionResult { ActionName: "Continue" } &&
    DoctorDashboard(LoginController(true).Continue()), "Signed-in doctor continues to the role dashboard");
Check(DoctorDashboard(await LoginController().Login("doctor@example.com", password, returnUrl: "/Account/Login")),
    "Login return URL does not redirect doctor back to login");
Check(DoctorDashboard(await LoginController().Login("doctor@example.com", password, returnUrl: "/Doctor/Requests")),
    "Login follows the existing restricted return URL policy");
Check(DoctorDashboard(await LoginController().Login("doctor@example.com", password, returnUrl: "https://example.com")),
    "Login rejects external return URL");
repository.State.MustChangePassword = true;
Check(await LoginController().Login("doctor@example.com", password) is RedirectToActionResult { ActionName: "ChangePassword" },
    "Temporary password still requires password change");
repository.State.SecurityStamp = Guid.NewGuid();
var signInsBefore = authentication.SignIns;
Check(await LoginController().Login("doctor@example.com", password) is ViewResult && authentication.SignIns == signInsBefore,
    "Mismatched security stamp still prevents sign-in");

Console.WriteLine($"{checks} authentication checks passed.");

sealed class FakeRepository : IAuthenticationRepository
{
    public User? LoginUser { get; set; }
    public AuthenticationState? State { get; set; }
    public int Lookups { get; private set; }
    public int ResetCalls { get; private set; }
    public string? LastTokenHash { get; private set; }
    public Task<User?> GetActiveUserByEmail(string email) { Lookups++; return Task.FromResult(LoginUser); }
    public Task<User?> GetActiveUserById(int id) => Task.FromResult(LoginUser);
    public Task<bool> UpdateProfile(int id, Guid stamp, string name, string surname, string email, string? phone, string? address) => Task.FromResult(true);
    public Task RecordLogin(int id) => Task.CompletedTask;
    public Task<AuthenticationState?> GetState(int id) => Task.FromResult(State);
    public Task<bool> CreateReset(int id, string hash) => Task.FromResult(true);
    public Task<bool> ResetPassword(string tokenHash, string hash, string salt)
    {
        ResetCalls++;
        LastTokenHash = tokenHash;
        return Task.FromResult(false);
    }
    public Task<bool> ChangePassword(int id, string old, string hash, string salt) => Task.FromResult(false);
}

sealed class TestUrlHelper : IUrlHelper
{
    public ActionContext ActionContext { get; } = new();
    public string? Action(UrlActionContext context) => $"/{context.Controller}/{context.Action}";
    public string? Content(string? path) => path;
    public bool IsLocalUrl(string? url) => url?.StartsWith('/') == true && !url.StartsWith("//") && !url.StartsWith("/\\");
    public string? Link(string? routeName, object? values) => null;
    public string? RouteUrl(UrlRouteContext context) => null;
}

sealed class FakeAuthenticationService : IAuthenticationService
{
    public int SignIns { get; private set; }
    public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme) => Task.FromResult(AuthenticateResult.NoResult());
    public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
    public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
    public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
    { SignIns++; return Task.CompletedTask; }
    public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties) => Task.CompletedTask;
}

sealed class TestActions : Microsoft.AspNetCore.Mvc.Infrastructure.IActionDescriptorCollectionProvider
{
    public Microsoft.AspNetCore.Mvc.Infrastructure.ActionDescriptorCollection ActionDescriptors => new(new[] {
        new Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor { ControllerName = "Doctor", ActionName = "Dashboard" }
    }, 0);
}
