using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Helpers;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AccountController(IAuthenticationRepository repository, PasswordResetQueue resetQueue,
    ILogger<AccountController> logger, AccountNavigation navigation) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        if (User.Identity?.IsAuthenticated == true)
            return User.HasClaim("MustChangePassword", "True")
                ? RedirectToAction(nameof(ChangePassword)) : RedirectToAction(nameof(Continue));
        return View();
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("account")]
    public async Task<IActionResult> Login(string? email, string? password, bool rememberMe = false, string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["Email"] = email;
        if (!ValidEmail(email) || string.IsNullOrEmpty(password) || password.Length > 128)
        {
            ModelState.AddModelError("", "Enter a valid email address and password.");
            return View();
        }
        var user = await repository.GetActiveUserByEmail(email!);
        if (user == null || !PasswordHelper.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
        {
            ModelState.AddModelError("", "The email address or password is incorrect.");
            return View();
        }
        var state = await repository.GetState(user.UserId);
        if (state == null || state.SecurityStamp != user.SecurityStamp)
        {
            ModelState.AddModelError("", "The email address or password is incorrect.");
            return View();
        }
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, $"{user.Name} {user.Surname}".Trim()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, state.Role),
            new("MustChangePassword", state.MustChangePassword.ToString()),
            new("SecurityStamp", state.SecurityStamp.ToString())
        };

        if (string.Equals(state.Role, "Patient", StringComparison.OrdinalIgnoreCase))
        {
            var patientId = await repository.GetPatientIdByUserId(user.UserId);

            if (!patientId.HasValue || patientId.Value <= 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Your patient account could not be verified. Please contact support.");

                return View();
            }

            claims.Add(new Claim("PatientID", patientId.Value.ToString()));
        }

        if (string.Equals(state.Role, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            var adminId = await repository.GetAdminIdByUserId(user.UserId);

            if (!adminId.HasValue || adminId.Value <= 0)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Your administrator account could not be verified. Please contact support.");

                return View();
            }

            claims.Add(new Claim("AdminID", adminId.Value.ToString()));
        }

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(rememberMe ? 24 * 14 : 8)
            });
        await repository.RecordLogin(user.UserId);
        if (state.MustChangePassword) return RedirectToAction(nameof(ChangePassword));
        var destination = navigation.Destination(state.Role);
        var destinationUrl = Url.Action(destination.Action, destination.Controller);
        if (Url.IsLocalUrl(returnUrl) &&
            (string.Equals(returnUrl, destinationUrl, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(returnUrl, Url.Action(nameof(Profile), "Account"), StringComparison.OrdinalIgnoreCase)))
            return LocalRedirect(returnUrl!);
        return RedirectToAction(destination.Action, destination.Controller);
    }

    [AllowAnonymous, HttpGet]
    public IActionResult ForgotPassword() => View();

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("account")]
    public IActionResult ForgotPassword(string? email)
    {
        if (!ValidEmail(email))
        {
            ModelState.AddModelError("", "Enter a valid email address.");
            return View();
        }
        if (!resetQueue.Enqueue(email!.Trim()))
            logger.LogWarning("Password reset request queue is full. Check email delivery health.");
        return RedirectToAction(nameof(ForgotPasswordConfirmation));
    }

    [AllowAnonymous, HttpGet]
    public IActionResult ForgotPasswordConfirmation() => View();

    [AllowAnonymous, HttpGet]
    public IActionResult ResetPassword(string? token)
    {
        ViewData["Token"] = token;
        if (!ValidToken(token)) ModelState.AddModelError("", "This reset link is invalid. Request a new link.");
        return View();
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("account")]
    public async Task<IActionResult> ResetPassword(string? token, string? password, string? confirmPassword)
    {
        ViewData["Token"] = token;
        if (!ValidToken(token)) ModelState.AddModelError("", "This reset link is invalid. Request a new link.");
        ValidateNewPassword(password, confirmPassword);
        if (!ModelState.IsValid) return View();
        var salt = PasswordHelper.GenerateSalt();
        if (!await repository.ResetPassword(TokenHash(token!), PasswordHelper.HashPassword(password!, salt), salt))
        {
            ModelState.AddModelError("", "This reset link has expired or has already been used. Request a new link.");
            return View();
        }
        await HttpContext.SignOutAsync();
        TempData["AccountMessage"] = "Your password has been reset. Sign in with your new password.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize, HttpGet]
    public IActionResult ChangePassword() => View();

    [Authorize, HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("account")]
    public async Task<IActionResult> ChangePassword(string? currentPassword, string? password, string? confirmPassword)
    {
        ValidateNewPassword(password, confirmPassword);
        if (string.IsNullOrEmpty(currentPassword) || currentPassword.Length > 128)
            ModelState.AddModelError("", "Enter your current password.");
        if (!ModelState.IsValid) return View();
        var user = await repository.GetActiveUserById(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
        if (user == null || !PasswordHelper.VerifyPassword(currentPassword!, user.PasswordHash, user.PasswordSalt))
        {
            ModelState.AddModelError("", "Your current password is incorrect.");
            return View();
        }
        if (PasswordHelper.VerifyPassword(password!, user.PasswordHash, user.PasswordSalt))
        {
            ModelState.AddModelError("", "Choose a password different from your current password.");
            return View();
        }
        var salt = PasswordHelper.GenerateSalt();
        if (!await repository.ChangePassword(user.UserId, user.PasswordHash, PasswordHelper.HashPassword(password!, salt), salt))
        {
            ModelState.AddModelError("", "Your account changed. Sign in again and retry.");
            return View();
        }
        await HttpContext.SignOutAsync();
        TempData["AccountMessage"] = "Your password has been changed. Sign in again.";
        return RedirectToAction(nameof(Login));
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous, HttpGet]
    public IActionResult AccessDenied() => View();

    [Authorize, HttpGet]
    public IActionResult Continue()
    {
        if (User.HasClaim("MustChangePassword", "True")) return RedirectToAction(nameof(ChangePassword));
        var destination = navigation.Destination(User);
        return RedirectToAction(destination.Action, destination.Controller);
    }

    [Authorize, HttpGet]
    public IActionResult Workspace() => View();

    [Authorize, HttpGet]
    public async Task<IActionResult> Profile()
    {
        var user = await repository.GetActiveUserById(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
        if (user == null) return Challenge();
        return View(user);
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("account")]
    public async Task<IActionResult> Profile(string? name, string? surname, string? email,
        string? cellphone, string? homeAddress, string? currentPassword)
    {
        var user = await repository.GetActiveUserById(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
        if (user == null) return Challenge();
        name = name?.Trim();
        surname = surname?.Trim();
        email = email?.Trim();
        cellphone = cellphone?.Trim();
        homeAddress = homeAddress?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100)
            ModelState.AddModelError("", "Enter your first name (up to 100 characters).");
        if (string.IsNullOrWhiteSpace(surname) || surname.Length > 100)
            ModelState.AddModelError("", "Enter your surname (up to 100 characters).");
        if ((!string.IsNullOrEmpty(name) && !System.Text.RegularExpressions.Regex.IsMatch(name, @"^[\p{L}\p{M} .'\u2019-]+$")) ||
            (!string.IsNullOrEmpty(surname) && !System.Text.RegularExpressions.Regex.IsMatch(surname, @"^[\p{L}\p{M} .'\u2019-]+$")))
            ModelState.AddModelError("", "Names and surnames must not contain numbers or symbols.");
        if (!ValidEmail(email)) ModelState.AddModelError("", "Enter a valid email address.");
        if (!string.IsNullOrEmpty(cellphone) && !System.Text.RegularExpressions.Regex.IsMatch(cellphone, "^[0-9]{10}$"))
            ModelState.AddModelError("", "Cellphone number must contain exactly 10 digits.");
        if (homeAddress?.Length > 255) ModelState.AddModelError("", "Home address must be 255 characters or fewer.");
        var emailChanged = !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase);
        if (emailChanged && (string.IsNullOrEmpty(currentPassword) || currentPassword.Length > 128 ||
            !PasswordHelper.VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt)))
            ModelState.AddModelError("", "Enter your current password to change your email address / username.");
        if (ModelState.IsValid)
        {
            try
            {
                if (await repository.UpdateProfile(user.UserId, user.SecurityStamp, name!, surname!, email!, cellphone, homeAddress))
                {
                    await HttpContext.SignOutAsync();
                    TempData["AccountMessage"] = "Your profile was saved. Sign in again using " + email + ".";
                    return RedirectToAction(nameof(Login));
                }
                ModelState.AddModelError("", "Your account changed while editing. Reload this page and try again.");
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 2601 or 2627)
            {
                ModelState.AddModelError("", "That email address is already in use. Choose another address.");
            }
        }
        user.Name = name ?? "";
        user.Surname = surname ?? "";
        user.Email = email ?? "";
        user.Cellphone = cellphone ?? "";
        user.HomeAddress = homeAddress ?? "";
        return View(user);
    }

    private void ValidateNewPassword(string? password, string? confirmation)
    {
        if (!PasswordHelper.MeetsPolicy(password))
            ModelState.AddModelError("", "Use 8–128 characters, including an uppercase letter, a number and a special character.");
        if (password != confirmation) ModelState.AddModelError("", "The passwords do not match.");
    }
    private static bool ValidEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email) && email.Trim().Length <= 150 && new EmailAddressAttribute().IsValid(email.Trim());
    private static bool ValidToken(string? token) => token is { Length: 64 } && token.All(Uri.IsHexDigit);
    private static string TokenHash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}


