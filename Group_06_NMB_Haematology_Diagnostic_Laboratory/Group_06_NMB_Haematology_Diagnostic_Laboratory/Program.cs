using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;



var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<IPasswordService, PasswordService>();
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<IPatientRepository, PatientRepository>();
// Data access
builder.Services.AddScoped<ISqlDataAccess, SqlDataAccess>();

// Patient repository
builder.Services.AddScoped<IPatientRepository, PatientRepository>();

// Add services to the container.
builder.Services.AddControllersWithViews();

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

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
