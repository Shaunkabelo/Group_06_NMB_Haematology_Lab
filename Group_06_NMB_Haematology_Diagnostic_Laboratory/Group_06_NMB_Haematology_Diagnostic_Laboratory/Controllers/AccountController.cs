using System.Security.Claims;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Helpers;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Group_06_NMB_Haematology_Diagnostic_Laboratory.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers;

public class AccountController : Controller
{
    private readonly IAuthenticationRepository _authenticationRepository;

    public AccountController(IAuthenticationRepository authenticationRepository)
    {
        _authenticationRepository = authenticationRepository;
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToDashboard(User.FindFirstValue(ClaimTypes.Role));
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _authenticationRepository.GetUserByEmail(model.Email);
        if (user != null && !user.IsActive)
        {
            ModelState.AddModelError(string.Empty,
                "Your account is blocked. Please contact the system administrator.");
            return View(model);
        }

        var validPassword = user != null &&
            PasswordHelper.VerifyPassword(model.Password, user.PasswordHash, user.PasswordSalt);

        if (!validPassword)
        {
            ModelState.AddModelError(string.Empty, "The email address or password is incorrect.");
            return View(model);
        }

        var role = user!.Role?.Trim() ?? string.Empty;
        if (role.Equals("Patient", StringComparison.OrdinalIgnoreCase)) role = "Patient";
        if (role.Equals("Admin", StringComparison.OrdinalIgnoreCase)) role = "Admin";

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user!.UserId.ToString()),
            new(ClaimTypes.Name, $"{user.Name} {user.Surname}".Trim()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, role),
            new("MustChangePassword", user.MustChangePassword.ToString())
        };

        if (role == "Patient")
        {
            var patientId = await _authenticationRepository.GetPatientIdByUserId(user.UserId);
            if (patientId is null || patientId <= 0)
            {
                ModelState.AddModelError(string.Empty,
                    "Your patient profile could not be found. Please contact the system administrator.");
                return View(model);
            }
            claims.Add(new Claim("PatientID", patientId.Value.ToString()));
        }

        if (role == "Admin")
        {
            var adminId = await _authenticationRepository.GetAdminIdByUserId(user.UserId);
            if (adminId is null || adminId <= 0)
            {
                ModelState.AddModelError(string.Empty,
                    "Your admin profile could not be found. Please contact the system administrator.");
                return View(model);
            }
            claims.Add(new Claim("AdminID", adminId.Value.ToString()));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme));

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = model.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(model.RememberMe ? 24 * 14 : 8)
            });

        await _authenticationRepository.RecordLogin(user.UserId);

        if (role != "Patient" && role != "Admin" && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToDashboard(role);
    }

    private IActionResult RedirectToDashboard(string? role) =>
        RedirectToAction("Dashboard", role?.Trim().ToLowerInvariant() switch
        {
            "patient" => "Patient",
            "admin" => "Admin",
            _ => "LabManager"
        });

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult AccessDenied() => View();
}
