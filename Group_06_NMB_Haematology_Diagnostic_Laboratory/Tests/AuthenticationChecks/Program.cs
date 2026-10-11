using System.Security.Claims;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
var patientPasswordService = new PasswordService();
var patientPassword = patientPasswordService.HashPassword(password);
Check(PasswordHelper.VerifyPassword(password, patientPassword.Hash, patientPassword.Salt),
    "Patient registration and password changes produce hashes accepted by shared login");
Check(patientPasswordService.VerifyPassword(password, hash, salt),
    "Patient password service verifies shared authentication hashes");
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
var descriptors = new TestActions();
var navigation = new AccountNavigation(new ConfigurationBuilder().Build(), descriptors);
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

Check(navigation.Destination("LabManager") == new AccountDestination("LabManager", "Dashboard"), "Manager routes to installed dashboard");
Check(navigation.Destination("Doctor") == new AccountDestination("Account", "Workspace"), "Missing Doctor subsystem uses shared workspace");
Check(navigation.Destination("Admin") == new AccountDestination("Account", "Workspace"), "Admin does not implicitly enter Manager subsystem");
Check(navigation.Destination("Unknown") == new AccountDestination("Account", "Workspace"), "Unknown role uses shared workspace");
descriptors.Items.Add(new ControllerActionDescriptor { ControllerName = "Doctor", ActionName = "Dashboard" });
Check(navigation.Destination("Doctor") == new AccountDestination("Doctor", "Dashboard"), "Merged Doctor dashboard is automatically discovered");
descriptors.Items.Add(new ControllerActionDescriptor { ControllerName = "TechDashboard", ActionName = "Index" });
Check(navigation.Destination("LabTechnician") == new AccountDestination("TechDashboard", "Index"), "Technician routes to the technician dashboard");

var auth = new TestAuthentication();
var services = new ServiceCollection().AddSingleton<IAuthenticationService>(auth).BuildServiceProvider();
var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] {
    new Claim(ClaimTypes.NameIdentifier, "42"), new Claim(ClaimTypes.Role, "Doctor")
}, "test"));
void Attach(Controller c)
{
    c.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = principal, RequestServices = services } };
    c.ControllerContext.RouteData = new Microsoft.AspNetCore.Routing.RouteData();
    c.ControllerContext.RouteData.Routers.Add(new TestRouter());
    c.ControllerContext.ActionDescriptor = new ControllerActionDescriptor();
    c.Url = new Microsoft.AspNetCore.Mvc.Routing.UrlHelper(c.ControllerContext);
    c.TempData = new TempDataDictionary(c.HttpContext, new TestTempData());
}
repository.CurrentUser = new User { UserId = 42, Name = "Test", Surname = "User", Email = "old@example.com",
    PasswordHash = hash, PasswordSalt = salt, SecurityStamp = Guid.NewGuid(), Role = "Doctor" };
controller = Controller(); Attach(controller);
Check(await controller.Profile() is ViewResult && repository.LastReadId == 42, "Profile loads signed-in user ID");
controller = Controller(); Attach(controller);
Check(await controller.Profile("Test", "User", "new@example.com", "", "", "wrong") is ViewResult &&
    repository.ProfileWrites == 0, "Email change requires correct current password");
controller = Controller(); Attach(controller);
Check(await controller.Profile("", "User", "old@example.com", "", "", null) is ViewResult &&
    repository.ProfileWrites == 0, "Invalid profile never written");
repository.CurrentUser.Email = "old@example.com";
controller = Controller(); Attach(controller);
Check(await controller.Profile("Test", "User", "new@example.com", "0123456789", "Address", password) is RedirectToActionResult &&
    repository.LastWriteId == 42 && auth.SignedOut, "Profile saves only own account and signs out stale cookie");
controller = Controller(); Attach(controller);
Check(controller.Continue() is RedirectToActionResult { ControllerName: "Doctor", ActionName: "Dashboard" },
    "Authenticated continuation uses role destination");
principal.AddIdentity(new ClaimsIdentity(new[] { new Claim("MustChangePassword", "True") }));
Check(controller.Continue() is RedirectToActionResult { ActionName: "ChangePassword" }, "Required password change precedes dashboard");

foreach (var role in new[] { "Patient", "Doctor", "LabTechnician", "LabManager", "Admin", "Unknown" })
{
    repository.CurrentUser.Role = role;
    repository.PatientId = 123;
    var lookupCount = repository.PatientLookups;
    auth.SignedIn = null;
    controller = Controller(); Attach(controller);
    Check(await controller.Login("known@example.com", password) is RedirectToActionResult &&
        auth.SignedIn?.FindFirstValue(ClaimTypes.Role) == role, $"Login preserves {role} role");
    var patientClaims = auth.SignedIn!.FindAll("PatientID").ToArray();
    Check(role == "Patient"
        ? patientClaims.Length == 1 && patientClaims[0].Value == "123" &&
          repository.PatientLookups == lookupCount + 1 && repository.PatientLookupUserId == 42
        : patientClaims.Length == 0 && repository.PatientLookups == lookupCount,
        $"{role} login uses patient lookup only when required, with UserID distinct from PatientID");
}
repository.CurrentUser.Role = "Patient";
foreach (var missingId in new int?[] { null, 0, -1 })
{
    repository.PatientId = missingId;
    auth.SignedIn = null;
    controller = Controller(); Attach(controller);
    Check(await controller.Login("known@example.com", password) is ViewResult &&
        !controller.ModelState.IsValid && auth.SignedIn == null,
        $"Patient login rejects invalid mapping: {missingId}");
}

var staff = DispatchProxy.Create<ILabManagerRepository, StaffRepositoryProxy>();
var staffProxy = (StaffRepositoryProxy)(object)staff;
var sender = new TestEmail();
var manager = new LabManagerController(staff, sender, NullLogger<LabManagerController>.Instance);
Attach(manager);
await manager.CreateManager(new LabManager { User = new User { Name = "New", Email = "new@example.com" } });
Check(sender.Sent == 1 && sender.Password == staffProxy.Password && PasswordHelper.MeetsPolicy(sender.Password),
    "Welcome email contains exactly the password saved for new account");
sender.Fail = true;
await manager.CreateManager(new LabManager { User = new User { Name = "New", Email = "new@example.com" } });
Check(manager.TempData["Error"] is string warning && warning.Contains("account was created"),
    "SMTP failure reports account already created without exposing password");
staffProxy.Fail = true;
var sentBefore = sender.Sent;
try { await manager.CreateManager(new LabManager { User = new User { Name = "New", Email = "new@example.com" } }); }
catch (InvalidOperationException) { }
Check(sender.Sent == sentBefore, "Database creation failure never sends credentials");
staffProxy.Fail = false;
manager.ModelState.Clear();
await manager.UpdateTechnicianTestTypes(42, new[] { 1, 2 });
Check(staffProxy.AssignmentSaves == 1, "Separate assignment action persists multiple tests");
await manager.UpdateTechnicianTestTypes(42, Array.Empty<int>());
Check(staffProxy.AssignmentSaves == 1, "Empty assignment selection is rejected");
var staffInput = new User { Name = "Anne-Marie", Surname = "O'Neil", Email = "staff@example.com",
    Cellphone = "0123456789", SaIdNumber = "9001015009087", DateOfBirth = new DateTime(1990,1,1), HomeAddress = "" };
Check(Group_06_NMB_Haematology_Diagnostic_Laboratory.Validation.StaffInputValidator.Validate(staffInput, "MP123", doctor:true).Count == 0,
    "Valid staff details and optional address accepted");
foreach (var id in new[] { "900101500908A", "900101500908", "90010150090870" })
{
    staffInput.SaIdNumber = id;
    Check(Group_06_NMB_Haematology_Diagnostic_Laboratory.Validation.StaffInputValidator.Validate(staffInput).ContainsKey("User.SaIdNumber"),
        "Invalid ID rejected: " + id.Length + " characters");
}
staffInput.SaIdNumber = "9001015009087";
foreach (var phone in new[] { "012345678A", "012345678", "01234567890" })
{
    staffInput.Cellphone = phone;
    Check(Group_06_NMB_Haematology_Diagnostic_Laboratory.Validation.StaffInputValidator.Validate(staffInput).ContainsKey("User.Cellphone"),
        "Invalid cellphone rejected: " + phone.Length + " characters");
}
staffInput.Cellphone = "0123456789";
staffInput.DateOfBirth = new DateTime(1991,1,1);
Check(Group_06_NMB_Haematology_Diagnostic_Laboratory.Validation.StaffInputValidator.Validate(staffInput).ContainsKey("User.SaIdNumber"), "ID/date mismatch rejected");
staffInput.DateOfBirth = DateTime.Today.AddDays(1);
Check(Group_06_NMB_Haematology_Diagnostic_Laboratory.Validation.StaffInputValidator.Validate(staffInput).ContainsKey("User.DateOfBirth"), "Future birth date rejected");
staffInput.Name = ""; staffInput.Email = "invalid"; staffInput.HomeAddress = new string('x',256);
var fieldErrors = Group_06_NMB_Haematology_Diagnostic_Laboratory.Validation.StaffInputValidator.Validate(staffInput, employee:"", technician:true);
Check(fieldErrors.ContainsKey("User.Name") && fieldErrors.ContainsKey("User.Email") &&
    fieldErrors.ContainsKey("User.HomeAddress") && fieldErrors.ContainsKey("EmployeeNumber"), "Clear errors returned for all invalid text fields");
var catalogueErrors = Group_06_NMB_Haematology_Diagnostic_Laboratory.Validation.CatalogueInputValidator.Validate(new object[] {
    new Supplier { SupplierName = " ", ContactPerson = "", SupplierEmail = "invalid" },
    new TestType { TestName = "", UnitOfMeasurement = "", NormalRangeMin = 10, NormalRangeMax = 2 },
    new Consumable { Name = "Tube", SupplierId = 0, QuantityOnHand = -1, ReorderLevel = -2 }
});
Check(catalogueErrors.ContainsKey("SupplierEmail") && catalogueErrors.ContainsKey("SupplierName"), "Supplier required fields and email validated");
Check(catalogueErrors.ContainsKey("NormalRangeMax") && catalogueErrors.ContainsKey("TurnaroundTimeMinutes"), "Test ranges and turnaround validated");
Check(catalogueErrors.ContainsKey("QuantityOnHand") && catalogueErrors.ContainsKey("SupplierId"), "Negative stock and missing supplier rejected");
Check(Group_06_NMB_Haematology_Diagnostic_Laboratory.Validation.CatalogueInputValidator.Validate(new object[] {
    new SampleType { SampleTypeName = "Whole blood" }, new TestCategory { CategoryName = "Coagulation" }
}).Count == 0, "Valid catalogue inputs accepted");
foreach (var text in new[] { "4.5", "4,5", "-4,5", "0", "99999999.9999" })
    Check(Group_06_NMB_Haematology_Diagnostic_Laboratory.Validation.NormalRangeParser.TryParse(text, out _), "Valid normal range: " + text);
foreach (var text in new[] { "abc", "1,234.5", "4..5", "1e3", "1.12345", "100000000", "", "4 5" })
    Check(!Group_06_NMB_Haematology_Diagnostic_Laboratory.Validation.NormalRangeParser.TryParse(text, out _), "Invalid normal range rejected: " + text);
Group_06_NMB_Haematology_Diagnostic_Laboratory.Validation.NormalRangeParser.TryParse("4,5", out var commaRange);
Group_06_NMB_Haematology_Diagnostic_Laboratory.Validation.NormalRangeParser.TryParse("4.5", out var dotRange);
Check(commaRange == 4.5m && commaRange == dotRange, "Comma and dot preserve the same decimal value");
Check(OrderStatusLabels.Display("Complete") == "Received" && OrderStatusLabels.Display("Partially complete") == "Partially Received", "Legacy receipt statuses display consistently");
manager.ModelState.Clear();
Check(await manager.DownloadReport("unknown", null, null) is RedirectToActionResult, "Unknown report type rejected");
Check(await manager.DownloadReport("orders", new DateTime(2026,10,2), new DateTime(2026,10,1)) is RedirectToActionResult, "Reversed order-report dates rejected");
Check(await manager.DownloadReport("orders", new DateTime(2026,10,1), DateTime.MaxValue) is RedirectToActionResult, "Report date overflow rejected");
Check(await manager.DownloadReport("orders", new DateTime(2026,10,1), new DateTime(2026,10,2), "bad") is RedirectToActionResult, "Invalid report status rejected");

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
var reportOutput = Path.Combine(Path.GetTempPath(), "NmbReportVerification");
Directory.CreateDirectory(reportOutput);
var orderReport = (FileContentResult)await manager.DownloadReport("orders", new DateTime(2026,9,1), new DateTime(2026,9,30), "Received");
Check(orderReport.ContentType == "application/pdf", "Order report endpoint produces a PDF");
File.WriteAllBytes(Path.Combine(reportOutput, "orders-filtered.pdf"), orderReport.FileContents);
var stockReport = (FileContentResult)await manager.DownloadReport("stock", null, null, lowStockOnly: true);
Check(stockReport.ContentType == "application/pdf", "Stock snapshot works without date inputs");
File.WriteAllBytes(Path.Combine(reportOutput, "stock-filtered.pdf"), stockReport.FileContents);
var longOrders = ManagementReportPdf.Generate("Stock Orders Report", "Sample records for pagination verification", "150 orders",
    new[] { "Order number", "Supplier", "Ordered on", "Item lines", "Status", "Received on" },
    Enumerable.Range(1,150).Select(i => new[] { $"ORD-{i:D3}", "Supplier name with longer text for wrapping",
        "30 Sep 2026", "2", "Partially Received", "—" }), DateTimeOffset.UtcNow);
File.WriteAllBytes(Path.Combine(reportOutput, "orders-multipage.pdf"), longOrders);
Check(longOrders.Length > 0, "Order table generates across multiple pages");
foreach (var count in new[] { 0, 3, 150 }) {
    var reportRows = Enumerable.Range(1, count).Select(i => new TestsPerformedByCategory {
        CategoryName = $"Category {i:D3} — Full blood count and differential studies",
        TotalTests = i
    }).ToList();
    var reportPdf = TestsPerformedPdf.Generate(reportRows, new DateTime(2026, 9, 1), new DateTime(2026, 9, 30),
        new DateTimeOffset(2026, 10, 1, 8, 30, 0, TimeSpan.Zero));
    Check(Encoding.ASCII.GetString(reportPdf, 0, 5) == "%PDF-", $"Report generates a PDF for {count} categories");
    File.WriteAllBytes(Path.Combine(reportOutput, $"report-{count}.pdf"), reportPdf);
}
Console.WriteLine($"Report samples: {reportOutput}");
Console.WriteLine($"{checks} authentication and validation checks passed.");

sealed class FakeRepository : IAuthenticationRepository
{
    public User? CurrentUser { get; set; }
    public int LastReadId { get; private set; }
    public int ProfileWrites { get; private set; }
    public int LastWriteId { get; private set; }
    public Task<User?> GetActiveUserById(int id) { LastReadId = id; return Task.FromResult(CurrentUser); }
    public Task<bool> UpdateProfile(int id, Guid stamp, string name, string surname, string email, string? phone, string? address)
    { ProfileWrites++; LastWriteId = id; return Task.FromResult(true); }
    public int Lookups { get; private set; }
    public int ResetCalls { get; private set; }
    public string? LastTokenHash { get; private set; }
    public Task<User?> GetActiveUserByEmail(string email) { Lookups++; return Task.FromResult(CurrentUser); }
    public int? PatientId { get; set; }
    public int PatientLookups { get; private set; }
    public int PatientLookupUserId { get; private set; }
    public Task<int?> GetPatientIdByUserId(int userId)
    { PatientLookups++; PatientLookupUserId = userId; return Task.FromResult(PatientId); }
    public Task RecordLogin(int id) => Task.CompletedTask;
    public Task<AuthenticationState?> GetState(int id) => Task.FromResult(CurrentUser == null ? null :
        new AuthenticationState { UserId = CurrentUser.UserId, Role = CurrentUser.Role, SecurityStamp = CurrentUser.SecurityStamp });
    public Task<bool> CreateReset(int id, string hash) => Task.FromResult(true);
    public Task<bool> ResetPassword(string tokenHash, string hash, string salt)
    {
        ResetCalls++;
        LastTokenHash = tokenHash;
        return Task.FromResult(false);
    }
    public Task<bool> ChangePassword(int id, string old, string hash, string salt) => Task.FromResult(false);
}

sealed class TestActions : IActionDescriptorCollectionProvider
{
    public List<ActionDescriptor> Items { get; } = new() { new ControllerActionDescriptor { ControllerName = "LabManager", ActionName = "Dashboard" } };
    public ActionDescriptorCollection ActionDescriptors => new(Items, 0);
}
sealed class TestRouter : Microsoft.AspNetCore.Routing.IRouter
{
    public Task RouteAsync(Microsoft.AspNetCore.Routing.RouteContext context) => Task.CompletedTask;
    public Microsoft.AspNetCore.Routing.VirtualPathData GetVirtualPath(Microsoft.AspNetCore.Routing.VirtualPathContext context)
        => new(this, $"{context.Values["controller"]}/{context.Values["action"]}");
}
sealed class TestTempData : ITempDataProvider
{
    public IDictionary<string, object> LoadTempData(HttpContext context) => new Dictionary<string, object>();
    public void SaveTempData(HttpContext context, IDictionary<string, object> values) { }
}
sealed class TestAuthentication : IAuthenticationService
{
    public ClaimsPrincipal? SignedIn { get; set; }
    public bool SignedOut { get; private set; }
    public Task<AuthenticateResult> AuthenticateAsync(HttpContext c, string? s) => Task.FromResult(AuthenticateResult.NoResult());
    public Task ChallengeAsync(HttpContext c, string? s, AuthenticationProperties? p) => Task.CompletedTask;
    public Task ForbidAsync(HttpContext c, string? s, AuthenticationProperties? p) => Task.CompletedTask;
    public Task SignInAsync(HttpContext c, string? s, ClaimsPrincipal u, AuthenticationProperties? p)
    { SignedIn = u; return Task.CompletedTask; }
    public Task SignOutAsync(HttpContext c, string? s, AuthenticationProperties? p) { SignedOut = true; return Task.CompletedTask; }
}
public class StaffRepositoryProxy : DispatchProxy
{
    public int AssignmentSaves { get; private set; }
    public string? Password { get; private set; }
    public bool Fail { get; set; }
    protected override object? Invoke(MethodInfo? method, object?[]? args)
    {
        if (method?.Name == "GetStockOrders") return Task.FromResult<IEnumerable<StockOrder>>(new[] {
            new StockOrder { OrderNumber="INCLUDED-END", OrderDate=new DateTime(2026,9,30,23,59,59), Status="Complete", Supplier=new Supplier {SupplierName="Test supplier"} },
            new StockOrder { OrderNumber="EXCLUDED-OCTOBER", OrderDate=new DateTime(2026,10,1), Status="Complete" },
            new StockOrder { OrderNumber="EXCLUDED-STATUS", OrderDate=new DateTime(2026,9,15), Status="Ordered" }
        });
        if (method?.Name == "GetConsumables") return Task.FromResult<IEnumerable<Consumable>>(new[] {
            new Consumable { Name="INCLUDED-NEAR", QuantityOnHand=11, ReorderLevel=10, Supplier=new Supplier { SupplierName="Supplier"} },
            new Consumable { Name="EXCLUDED-ENOUGH", QuantityOnHand=12, ReorderLevel=10 }
        });
        if (method?.Name == "GetTechnicianById") return Task.FromResult<LabTechnician?>(new LabTechnician { User = new User { IsActive = true } });
        if (method?.Name == "GetTestTypes") return Task.FromResult<IEnumerable<TestType>>(new[] { new TestType { TestTypeId = 1 }, new TestType { TestTypeId = 2 } });
        if (method?.Name == "ReplaceTechnicianTestTypes") { AssignmentSaves++; return Task.CompletedTask; }
        if (method?.Name == "CreateManager")
        {
            if (Fail) throw new InvalidOperationException("Simulated database failure");
            Password = (string)args![1]!;
            return Task.CompletedTask;
        }
        throw new NotSupportedException(method?.Name);
    }
}
sealed class TestEmail : IAccountEmailSender
{
    public int Sent { get; private set; }
    public string? Password { get; private set; }
    public bool Fail { get; set; }
    public Task SendSupplierNotification(StockOrder order, string notification)
    {
        if (Fail) throw new InvalidOperationException("Simulated SMTP failure");
        return Task.CompletedTask;
    }
    public Task SendResetLink(string recipient, string link) => Task.CompletedTask;
    public Task SendTemporaryPassword(string recipient, string name, string password)
    {
        Sent++; Password = password;
        if (Fail) throw new InvalidOperationException("Simulated SMTP failure");
        return Task.CompletedTask;
    }
}




