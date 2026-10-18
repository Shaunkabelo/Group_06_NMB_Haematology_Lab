using Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using QuestPDF.Infrastructure;
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

var builder = WebApplication.CreateBuilder(args);
QuestPDF.Settings.License = LicenseType.Community;

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<Group_06_NMB_Haematology_Diagnostic_Laboratory.Validation.StaffValidationFilter>();
builder.Services.AddScoped<IAccountEmailSender, SmtpAccountEmailSender>();
builder.Services.AddScoped<AccountCookieEvents>();
builder.Services.AddScoped<AccountNavigation>();
builder.Services.AddSingleton<PasswordResetQueue>();
builder.Services.AddHostedService<PasswordResetWorker>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("account", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddScoped<ISqlDataAccess, SqlDataAccess>();
builder.Services.AddScoped<ILabManagerRepository, LabManagerRepository>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IDoctorResultsRepository, DoctorResultsRepository>();
builder.Services.AddScoped<IPatientRegistrationRepository, PatientRegistrationRepository>();
builder.Services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTechnicianService, CurrentTechnicianService>();
builder.Services.AddScoped<ITechDashboardRepository, TechDashboardRepository>();
builder.Services.AddScoped<ITechProcessRepository, TechProcessRepository>();
builder.Services.AddScoped<ITechReceiveRepository, TechReceiveRepository>();
builder.Services.AddScoped<ITechResultsRepository, TechResultsRepository>();
builder.Services.AddScoped<ITechVerifyRepository, TechVerifyRepository>();
builder.Services.AddScoped<ITechReportsRepository, TechReportsRepository>();
builder.Services.AddScoped<ITechProfileRepository, TechProfileRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IPasswordService, PasswordService>();
// Patient subsystem - email services
builder.Services.AddScoped<IRegistrationEmailService, RegistrationEmailService>();
builder.Services.AddScoped<IConsentEmailService, ConsentEmailService>();
builder.Services.AddScoped<SmtpEmailSender>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.EventsType = typeof(AccountCookieEvents);
    });

var app = builder.Build();


// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    if (context.User.Identity?.IsAuthenticated == true &&
        context.User.HasClaim("MustChangePassword", "True"))
    {
        var action = context.Request.RouteValues["action"]?.ToString();
        var controller = context.Request.RouteValues["controller"]?.ToString();
        if (!string.Equals(controller, "Account", StringComparison.OrdinalIgnoreCase) ||
            !(string.Equals(action, "ChangePassword", StringComparison.OrdinalIgnoreCase) ||
              string.Equals(action, "Logout", StringComparison.OrdinalIgnoreCase)))
        {
            context.Response.Redirect(context.Request.PathBase + "/Account/ChangePassword");
            return;
        }
    }
    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

