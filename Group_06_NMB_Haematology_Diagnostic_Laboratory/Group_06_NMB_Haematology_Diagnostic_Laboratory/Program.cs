using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
//builder.Services.AddTransient<ISqlDataAccess, SqlDataAccess>();
//builder.Services.AddTransient<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IDoctorResultsRepository,DoctorResultsRepository>();
builder.Services.AddScoped<ISqlDataAccess, SqlDataAccess>();
builder.Services.AddScoped<IDoctorRepository, DoctorRepository>();
builder.Services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();
//Patient Service
builder.Services
	.AddAuthentication(
		"MyCookieAuth")
	.AddCookie(
		"MyCookieAuth",
		options =>
		{
			options.LoginPath =
				"/Account/Login";

			options.AccessDeniedPath =
				"/Account/AccessDenied";

			options.ExpireTimeSpan =
				TimeSpan.FromHours(8);
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
	pattern: "{controller=Doctor}/{action=Patients}/{id?}");

app.Run();
