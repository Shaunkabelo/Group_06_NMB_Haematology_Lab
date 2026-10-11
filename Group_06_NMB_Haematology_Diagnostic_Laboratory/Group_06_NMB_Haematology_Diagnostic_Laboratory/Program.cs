using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.SqlClient;



var builder = WebApplication.CreateBuilder(args);

// Allow the development SQL server's untrusted certificate without disabling encryption.
if (builder.Environment.IsDevelopment() &&
    builder.Configuration.GetValue<bool>("Database:TrustServerCertificate"))
{
    var connectionString = builder.Configuration.GetConnectionString("conn");
    if (!string.IsNullOrWhiteSpace(connectionString))
    {
        var sqlConnection = new SqlConnectionStringBuilder(connectionString)
        {
            Encrypt = SqlConnectionEncryptOption.Mandatory,
            TrustServerCertificate = true
        };
        builder.Configuration["ConnectionStrings:conn"] = sqlConnection.ConnectionString;
    }
}

builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddScoped<IConsentEmailService, ConsentEmailService>();
builder.Services.AddScoped<IRegistrationEmailService, RegistrationEmailService>();
builder.Services.AddControllersWithViews();

// Data access
builder.Services.AddScoped<ISqlDataAccess, SqlDataAccess>();

// Patient repository
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
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

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
