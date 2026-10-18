using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Dapper;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Helpers;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers;
using Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;

// Explicit opt-in, metadata only. Never creates or updates database objects or patient records.
if (args.Contains("--database-schema") || args.Contains("--database-read-checks"))
{
    var config = new ConfigurationBuilder()
        .AddJsonFile(Path.GetFullPath("Group_06_NMB_Haematology_Diagnostic_Laboratory/appsettings.json"), optional: true)
        .AddUserSecrets(typeof(DoctorController).Assembly).AddEnvironmentVariables().Build();
    using var db = new SqlConnection(config.GetConnectionString("conn"));
    try
    {
        await db.OpenAsync();
        if (args.Contains("--database-read-checks"))
        {
            var repository = new DoctorWorkflowRepository(config);
            // Negative IDs exercise SQL against the schema without returning real patient records.
            await repository.Requests(-1);
            await repository.Requests(-1, -1);
            await repository.Request(-1, -1);
            await repository.Items(-1, -1);
            await repository.Results(-1, -1);
            await repository.Alerts(-1, new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));
            await repository.Patient(-1);
            var readySql = (string)typeof(DoctorWorkflowRepository).GetField("Ready", BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!;
            await db.ExecuteAsync("sys.sp_describe_first_result_set", new { tsql = readySql, @params = "@doctorId int, @requestId int" },
                commandType: System.Data.CommandType.StoredProcedure);
            // SQL-only fixtures use CTE literals, not real patient data or temporary/permanent writes.
            var readyFixture = """
                ;WITH TestRequest AS (SELECT 12 TestRequestID, 3 DoctorID, 'Completed' Status),
                TestRequestTestType AS (SELECT 101 TestRequestTestTypeID, 12 TestRequestID, @TestStatus Status
                    UNION ALL SELECT 102, 12, 'Verified' WHERE @MissingTest = 1),
                LabTest AS (SELECT 201 LabTestID, 101 TestRequestTestTypeID),
                TestResult AS (SELECT 301 TestResultID, 201 LabTestID, 10 CapturedByLabTechnicianID,
                    CAST('2026-10-18T10:00:00' AS datetime2) CaptureDateTime),
                TestResultVerification AS (SELECT 401 TestResultVerificationID, 301 TestResultID,
                    @Verifier VerifiedByLabTechnicianID, @VerificationStatus Status,
                    DATEADD(HOUR, @VerificationOffset, CAST('2026-10-18T10:00:00' AS datetime2)) VerificationDateTime)
                """ + readySql.Replace("dbo.", "").Replace(" WITH (UPDLOCK, HOLDLOCK)", "");
            foreach (var scenario in new[] {
                ("Verified", "Verified", 11, 1, 0, 1),
                ("Completed", "Verified", 11, 1, 0, 0),
                ("Verified", "To Be Reviewed", 11, 1, 0, 0),
                ("Verified", "Verified", 10, 1, 0, 0),
                ("Verified", "Verified", 11, -1, 0, 0),
                ("Verified", "Verified", 11, 1, 1, 0)
            })
            {
                var result = await db.ExecuteScalarAsync<int>(readyFixture, new { doctorId = 3, requestId = 12,
                    TestStatus = scenario.Item1, VerificationStatus = scenario.Item2, Verifier = scenario.Item3,
                    VerificationOffset = scenario.Item4, MissingTest = scenario.Item5 });
                if (result != scenario.Item6) throw new InvalidOperationException("Release SQL fixture failed.");
            }
            var accessSql = (string)typeof(DoctorWorkflowRepository).GetField("TestAccess", BindingFlags.NonPublic | BindingFlags.Static)!.GetRawConstantValue()!;
            var accessFixture = """
                ;WITH PatientConsent AS (SELECT 1 PatientConsentID, 7 PatientID, 3 DoctorID,
                    @Active IsActive, @GrantedTest TestRequestTestTypeID),
                PatientConsentRequest AS (SELECT 1 PatientConsentID, 12 TestRequestID WHERE @Legacy = 1),
                TestRequest AS (SELECT 12 TestRequestID, 7 PatientID, @Owner DoctorID, 'Released by doctor' Status),
                TestRequestTestType AS (SELECT 101 TestRequestTestTypeID UNION ALL SELECT 102)
                SELECT COUNT(*) FROM TestRequest r CROSS JOIN TestRequestTestType rt WHERE
                """ + accessSql.Replace("dbo.", "");
            foreach (var scenario in new[] { (1, (int?)101, 0, 8, 1), (0, (int?)101, 0, 8, 0),
                (1, (int?)null, 1, 8, 2), (1, (int?)101, 1, 8, 1), (0, (int?)null, 0, 3, 2) })
            {
                var count = await db.ExecuteScalarAsync<int>(accessFixture, new { DoctorID = 3,
                    Active = scenario.Item1, GrantedTest = scenario.Item2, Legacy = scenario.Item3, Owner = scenario.Item4 });
                if (count != scenario.Item5) throw new InvalidOperationException("Consent SQL fixture failed.");
            }
            Console.WriteLine("PASS: 11 SQL fixtures verify release completeness, independent/current verification, test-scoped consent, revocation, and legacy grants.");
            var missing = await db.QueryAsync<string>("""
                SELECT name FROM (VALUES ('sp_Auth_GetUserById'), ('sp_Auth_UpdateProfile'),
                    ('sp_Auth_ChangePassword'), ('spDoctor_GetByUserId'), ('spTestRequest_CancelByDoctor')) required(name)
                WHERE OBJECT_ID('dbo.' + QUOTENAME(name), 'P') IS NULL
                """);
            foreach (var procedure in missing) Console.WriteLine("DEPLOYMENT REQUIREMENT: missing procedure " + procedure);
            Console.WriteLine("PASS: request, consent, item, result, alert, patient and release-validation SQL compile against the configured schema. No data was changed.");
            return;
        }
        var rows = await db.QueryAsync<string>("""
            SELECT t.name + '.' + c.name + ' ' + TYPE_NAME(c.user_type_id) + ' (' + CAST(c.max_length AS varchar(10)) + ')'
            FROM sys.tables t INNER JOIN sys.columns c ON c.object_id = t.object_id
            WHERE t.name IN ('User','Patient','Doctor','TestRequest','TestRequestTestType','Sample','TestType',
                'LabTest','TestResult','TestResultVerification','PatientConsent','PatientConsentRequest','DoctorEmailQueue')
            ORDER BY t.name, c.column_id
            """);
        foreach (var row in rows) Console.WriteLine(row);
        var constraints = await db.QueryAsync<string>("""
            SELECT OBJECT_NAME(parent_object_id) + ': ' + definition FROM sys.check_constraints
            WHERE OBJECT_NAME(parent_object_id) IN ('User','TestRequest','TestRequestTestType','TestResultVerification')
            UNION ALL
            SELECT OBJECT_NAME(c.object_id) + '.' + c.name + ': nullable=' + CAST(c.is_nullable AS varchar(1))
              + ' default=' + COALESCE(dc.definition, 'none')
            FROM sys.columns c LEFT JOIN sys.default_constraints dc ON dc.object_id = c.default_object_id
            WHERE OBJECT_NAME(c.object_id) IN ('PatientConsent','User','TestRequest','TestRequestTestType')
            """);
        foreach (var constraint in constraints) Console.WriteLine(constraint);
        var procedures = await db.QueryAsync<string>("""
            SELECT OBJECT_NAME(object_id) + CHAR(10) + definition FROM sys.sql_modules
            WHERE OBJECT_NAME(object_id) LIKE '%Consent%'
              OR OBJECT_NAME(object_id) IN ('spDoctor_GetByUserId','spPatient_GetAll','spPatient_GetBySaId')
            """);
        foreach (var procedure in procedures) Console.WriteLine(procedure);
    }
    catch { Console.Error.WriteLine("Read-only schema check could not connect. Check ConnectionStrings:conn and SQL Server availability."); Environment.ExitCode = 1; }
    return;
}

var checks = 0;
void Check(bool value, string message)
{
    if (!value) throw new Exception("FAILED: " + message);
    checks++; Console.WriteLine("PASS: " + message);
}
var state = new Fixture();
var workflow = Proxy<IDoctorWorkflowRepository>.Create((name, a) => name switch
{
    "Requests" => Task.FromResult<IEnumerable<DoctorRequestRecord>>(new[] { state.Request }),
    "Request" => Task.FromResult((int)a[1]! == 12 ? state.Request : null),
    "Items" => Task.FromResult<IEnumerable<DoctorRequestItem>>(new[] { new DoctorRequestItem { TestType = "Count", Status = "Verified" } }),
    "Results" => Task.FromResult<IEnumerable<DoctorResultRowDto>>(new[] { state.Result }),
    "Alerts" => state.Alerts((DateTime)a[1]!, (DateTime)a[2]!),
    "Patient" => Task.FromResult<PatientDetailsDto?>(new PatientDetailsDto { PatientId = 7, Email = "patient@example.com" }),
    "Create" => state.Create((int)a[0]!, (CreateTestRequestModel)a[1]!, (string)a[2]!),
    "Register" => state.Register((PatientRegistrationViewModel)a[0]!, (string)a[1]!, (string)a[2]!, (string)a[3]!),
    "Release" => state.Release((int)a[0]!, (string)a[3]!),
    "EmailResults" => Task.FromResult(true),
    _ => throw new NotSupportedException(name)
});
var doctors = Proxy<IDoctorRepository>.Create((name, a) => name switch
{
    "GetDoctorIdByUserIdAsync" => Task.FromResult<int?>((int)a[0]! == 42 ? 3 : null),
    "GetPatientsAsync" => Task.FromResult<IEnumerable<Patient>>(Array.Empty<Patient>()),
    "CancelTestRequestAsync" => Task.FromResult(1),
    _ => throw new NotSupportedException(name)
});
var auth = Proxy<IAuthenticationRepository>.Create((name, a) => name switch
{
    "GetActiveUserById" => Task.FromResult<User?>(state.User),
    "ChangePassword" => state.ChangePassword((string)a[2]!, (string)a[3]!),
    _ => throw new NotSupportedException(name)
});

var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
builder.Logging.ClearProviders();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Configuration["AccountSecurity:PublicBaseUrl"] = "https://example.test";
builder.Services.AddSingleton(workflow).AddSingleton(doctors).AddSingleton(auth);
builder.Services.AddSingleton<IDataProtectionProvider>(new EphemeralDataProtectionProvider());
builder.Services.AddSingleton<DoctorEmailProtector>();
builder.Services.AddControllersWithViews().AddApplicationPart(typeof(DoctorController).Assembly);
builder.Services.AddAuthentication("Test").AddScheme<AuthenticationSchemeOptions, TestAuthentication>("Test", _ => { });
builder.Services.AddRateLimiter(options => options.AddPolicy("account", _ =>
    System.Threading.RateLimiting.RateLimitPartition.GetNoLimiter("tests")));
await using var app = builder.Build();
app.UseRouting(); app.UseAuthentication(); app.UseAuthorization(); app.UseRateLimiter();
app.MapControllerRoute("default", "{controller=Doctor}/{action=Dashboard}/{id?}");
app.MapGet("/test-token", (HttpContext c, IAntiforgery antiforgery) => Results.Text(antiforgery.GetAndStoreTokens(c).RequestToken!));
await app.StartAsync();
var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.Single();
using var handler = new HttpClientHandler { AllowAutoRedirect = false, CookieContainer = new CookieContainer() };
using var client = new HttpClient(handler) { BaseAddress = new Uri(address) };
Check((await client.GetAsync("/Doctor/GetPatients")).StatusCode == HttpStatusCode.Unauthorized, "Anonymous patient reads are rejected");
client.DefaultRequestHeaders.Add("X-Test-Role", "Patient");
Check((await client.GetAsync("/Doctor/GetPatients")).StatusCode == HttpStatusCode.Forbidden, "Other roles cannot read Doctor patient endpoints");
client.DefaultRequestHeaders.Remove("X-Test-Role"); client.DefaultRequestHeaders.Add("X-Test-Role", "Doctor");
Check((await client.GetAsync("/Doctor/GetPatients")).IsSuccessStatusCode, "Authorized Doctor can read patients");
Check((await client.PostAsJsonAsync("/Doctor/CreateTestRequest", new { patientId = 7 })).StatusCode == HttpStatusCode.BadRequest,
    "Doctor POST without antiforgery token is rejected");
var token = await client.GetStringAsync("/test-token");
client.DefaultRequestHeaders.Add("RequestVerificationToken", token);

var valid = new CreateTestRequestModel { PatientId = 7, Urgency = "STAT", ClinicalNotes = "Check count",
    Tests = new() { new() { TestTypeId = 1, SampleTypeId = 2, BarcodeNumber = " A123 " } } };
valid.Urgency = "invalid";
Check((await client.PostAsJsonAsync("/Doctor/CreateTestRequest", valid)).StatusCode == HttpStatusCode.BadRequest && state.Creates == 0,
    "Invalid urgency never reaches database");
valid.Urgency = "STAT"; valid.Tests.Clear();
Check((await client.PostAsJsonAsync("/Doctor/CreateTestRequest", valid)).StatusCode == HttpStatusCode.BadRequest, "Request must contain a test");
valid.Tests.Add(new() { TestTypeId = 1, SampleTypeId = 2, BarcodeNumber = "" });
Check((await client.PostAsJsonAsync("/Doctor/CreateTestRequest", valid)).StatusCode == HttpStatusCode.BadRequest, "Every selected test needs a sample barcode");
valid.Tests[0].BarcodeNumber = " A123 ";
valid.Tests.Add(new() { TestTypeId = 1, SampleTypeId = 2, BarcodeNumber = "B123" });
Check((await client.PostAsJsonAsync("/Doctor/CreateTestRequest", valid)).StatusCode == HttpStatusCode.BadRequest, "Duplicate tests are rejected");
valid.Tests.RemoveAt(1);
Check((await client.PostAsJsonAsync("/Doctor/CreateTestRequest", valid)).IsSuccessStatusCode && state.Creates == 1 && state.ActorDoctorId == 3,
    "Request creation resolves DoctorID from UserID and queues notification");
var protector = app.Services.GetRequiredService<DoctorEmailProtector>();
Check(protector.Read(state.Notification).Recipient == "patient@example.com", "Request email is addressed to the selected patient");

Check((await client.GetAsync("/Doctor/GetRequestData?id=999")).StatusCode == HttpStatusCode.NotFound, "Unavailable request details are not exposed");
Check((await client.GetAsync("/Doctor/DownloadResultPdf?id=999")).StatusCode == HttpStatusCode.NotFound, "Unavailable result PDFs are not exposed");
var pdf = await client.GetByteArrayAsync("/Doctor/DownloadResultPdf?id=12");
Check(Encoding.ASCII.GetString(pdf).StartsWith("%PDF-"), "Result PDF endpoint returns a PDF");
Check(Encoding.ASCII.GetString(pdf).Contains("ABNORMAL"), "Result PDF marks abnormal results");
var delivery = new DoctorResultMessage { TestRequestId = 12, Note = "Please review with me", Delivery = "Appointment" };
state.Request.DoctorId = 8;
Check((await client.PostAsJsonAsync("/Doctor/ReleaseResults", delivery)).StatusCode == HttpStatusCode.NotFound && state.Releases == 0,
    "Consent to read does not allow release of another doctor's request");
state.Request.DoctorId = 3; state.Ready = false;
Check((await client.PostAsJsonAsync("/Doctor/ReleaseResults", delivery)).StatusCode == HttpStatusCode.Conflict, "Release failure is reported without pretending success");
state.Ready = true;
Check((await client.PostAsJsonAsync("/Doctor/ReleaseResults", delivery)).IsSuccessStatusCode, "Completed verified request can be released");
Check(!protector.Read(state.Notification).AttachResults, "Appointment email excludes result attachment");
delivery.Delivery = "Results";
Check((await client.PostAsJsonAsync("/Doctor/ReleaseResults", delivery)).IsSuccessStatusCode && protector.Read(state.Notification).AttachResults,
    "Results email includes a PDF attachment instruction");
delivery.Note = " ";
Check((await client.PostAsJsonAsync("/Doctor/ReleaseResults", delivery)).StatusCode == HttpStatusCode.BadRequest, "Release requires a patient note");
Check((await client.GetAsync("/Doctor/GetDoctorAlerts?startDate=2026-10-14&endDate=2026-10-18")).IsSuccessStatusCode &&
    state.Start == new DateTime(2026,10,14) && state.End == new DateTime(2026,10,19), "Alerts include every time on the final day");
Check((await client.GetAsync("/Doctor/GetDoctorAlerts?startDate=2026-10-19&endDate=2026-10-18")).StatusCode == HttpStatusCode.BadRequest,
    "Reversed alert date range is rejected");

var registration = new Dictionary<string,string> { ["Name"] = "Test", ["Surname"] = "Patient", ["Email"] = "patient@example.com",
    ["Cellphone"] = "0123456789", ["HomeAddress"] = "Test street", ["SaIdNumber"] = "9001015009087", ["DateOfBirth"] = "1990-01-01" };
var registered = await client.PostAsync("/Doctor/RegisterPatient", new FormUrlEncodedContent(registration));
Check(registered.IsSuccessStatusCode && state.Registrations == 1, "Doctor registration succeeds without user-supplied password");
Check(PasswordHelper.MeetsPolicy(state.TemporaryPassword) && PasswordHelper.VerifyPassword(state.TemporaryPassword, state.Hash, state.Salt),
    "Generated patient password meets policy and verifies through shared login");
Check(protector.Read(state.Notification).Body.Contains(state.TemporaryPassword) && !state.Notification.Contains(state.TemporaryPassword),
    "Queued credentials are encrypted at rest and recoverable by the sender");
registration["DateOfBirth"] = "1991-01-01";
Check((await client.PostAsync("/Doctor/RegisterPatient", new FormUrlEncodedContent(registration))).StatusCode == HttpStatusCode.BadRequest,
    "Patient date of birth must match SA ID");
Check((await client.PostAsJsonAsync("/Doctor/ChangeDoctorPassword", new ChangePasswordDto {
    CurrentPassword = "SecurePass8!", NewPassword = "weakweak", ConfirmPassword = "weakweak" })).StatusCode == HttpStatusCode.BadRequest,
    "Doctor password changes enforce full server-side policy");
Check((await client.PostAsJsonAsync("/Doctor/ChangeDoctorPassword", new ChangePasswordDto {
    CurrentPassword = "SecurePass8!", NewPassword = "UpdatedPass9!", ConfirmPassword = "UpdatedPass9!" })).IsSuccessStatusCode &&
    PasswordHelper.VerifyPassword("UpdatedPass9!", state.Hash, state.Salt), "Doctor password changes use the shared authentication format");

foreach (var page in new[] { "Requests", "RequestDetails?id=12", "Results", "Patients", "CreateRequest", "RegisterPatient", "Alerts", "Reports" })
    Check((await client.GetAsync("/Doctor/" + page)).IsSuccessStatusCode, "Razor page renders: " + page);

// Exercise the real queue worker with a fake transport: no SMTP connection is made.
foreach (var failDelivery in new[] { false, true })
{
    var completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    var claimed = false;
    var job = new DoctorEmailJob { Id = 1, Kind = "Release", TestRequestId = 12,
        Payload = protector.Protect(new DoctorEmail("patient@example.com", "Results", "Review these results", 3, true)) };
    var queue = Proxy<IDoctorWorkflowRepository>.Create((name, a) => name switch
    {
        "ClaimEmail" => Task.FromResult<DoctorEmailJob?>(claimed ? null : Claim()),
        "Request" => Task.FromResult<DoctorRequestRecord?>(new DoctorRequestRecord { Id = 12, DoctorId = 3, Status = "Released by doctor" }),
        "Results" => Task.FromResult<IEnumerable<DoctorResultRowDto>>(new[] { state.Result }),
        "CompleteEmail" => Complete((bool)a[1]!),
        _ => throw new NotSupportedException(name)
    });
    DoctorEmailJob Claim() { claimed = true; return job; }
    Task Complete(bool delivered) { completed.TrySetResult(delivered); return Task.CompletedTask; }
    var sender = new RecordingSender { Fail = failDelivery };
    using var workerServices = new ServiceCollection().AddSingleton(queue).AddSingleton(protector)
        .AddSingleton<IDoctorEmailSender>(sender).BuildServiceProvider();
    using var worker = new DoctorEmailWorker(workerServices.GetRequiredService<IServiceScopeFactory>(),
        Microsoft.Extensions.Logging.Abstractions.NullLogger<DoctorEmailWorker>.Instance);
    await worker.StartAsync(CancellationToken.None);
    var delivered = await completed.Task.WaitAsync(TimeSpan.FromSeconds(10));
    await worker.StopAsync(CancellationToken.None);
    Check(delivered == !failDelivery, failDelivery ? "Failed delivery stays queued for retry" : "Successful delivery is marked complete");
    Check(sender.Attachment is { Length: > 0 }, "Worker generates the result PDF attachment");
}

var pdfPath = Path.Combine(Path.GetTempPath(), "doctor-workflow-results-check.pdf");
File.WriteAllBytes(pdfPath, DoctorRequestsPdfReport.BuildResults(state.Request, Enumerable.Repeat(state.Result, 100)));
Console.WriteLine("PDF validation sample: " + pdfPath);
Console.WriteLine($"{checks} Doctor workflow checks passed. No database records or emails were sent.");
await app.StopAsync();

public class Proxy<T> : DispatchProxy where T : class
{
    private Func<string, object?[], object?> handler = null!;
    public static T Create(Func<string, object?[], object?> handler)
    { var proxy = Create<T, Proxy<T>>(); ((Proxy<T>)(object)proxy).handler = handler; return proxy; }
    protected override object? Invoke(MethodInfo? method, object?[]? args) => handler(method!.Name, args ?? Array.Empty<object?>());
}
sealed class Fixture
{
    public int Creates, Releases, Registrations, ActorDoctorId;
    public bool Ready = true;
    public DateTime Start, End;
    public string Notification = "", Hash = "", Salt = "", TemporaryPassword = "";
    public User User { get; } = new() { UserId = 42, PasswordSalt = PasswordHelper.GenerateSalt() };
    public Fixture() { User.PasswordHash = PasswordHelper.HashPassword("SecurePass8!", User.PasswordSalt); }
    public DoctorRequestRecord Request = new() { Id = 12, DoctorId = 3, PatientId = 7, PatientName = "Test Patient", Email = "patient@example.com", Status = "Completed" };
    public DoctorResultRowDto Result = new() { TestRequestId = 12, TestName = "Blood count", ResultValue = 4, IsAbnormal = true, VerificationStatus = "Verified" };
    public Task<int> Create(int doctorId, CreateTestRequestModel model, string notification)
    { Creates++; ActorDoctorId = doctorId; Notification = notification; return Task.FromResult(12); }
    public Task<string> Register(PatientRegistrationViewModel model, string hash, string salt, string notification)
    { Registrations++; Hash = hash; Salt = salt; TemporaryPassword = model.Password; Notification = notification; return Task.FromResult("SUCCESS"); }
    public Task<bool> Release(int doctorId, string notification)
    { Releases++; ActorDoctorId = doctorId; Notification = notification; return Task.FromResult(Ready); }
    public Task<bool> ChangePassword(string hash, string salt) { Hash = hash; Salt = salt; return Task.FromResult(true); }
    public Task<IEnumerable<DoctorAlertDto>> Alerts(DateTime start, DateTime end)
    { Start = start; End = end; return Task.FromResult<IEnumerable<DoctorAlertDto>>(Array.Empty<DoctorAlertDto>()); }
}
sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder), IAuthenticationSignOutHandler
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var role = Request.Headers["X-Test-Role"].ToString();
        if (string.IsNullOrEmpty(role)) return Task.FromResult(AuthenticateResult.NoResult());
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "42"),
            new Claim(ClaimTypes.Name, "Test Doctor"), new Claim(ClaimTypes.Role, role) }, Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(user, Scheme.Name)));
    }
    public Task SignOutAsync(AuthenticationProperties? properties) => Task.CompletedTask;
}
sealed class RecordingSender : IDoctorEmailSender
{
    public bool Fail { get; set; }
    public byte[]? Attachment { get; private set; }
    public Task Send(DoctorEmail email, byte[]? pdf = null, CancellationToken cancellationToken = default)
    {
        Attachment = pdf;
        return Fail ? Task.FromException(new InvalidOperationException("Simulated SMTP failure")) : Task.CompletedTask;
    }
}
