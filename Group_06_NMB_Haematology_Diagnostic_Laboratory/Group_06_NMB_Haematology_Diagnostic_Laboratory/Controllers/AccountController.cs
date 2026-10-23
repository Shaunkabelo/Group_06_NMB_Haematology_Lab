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

            claims.Add(new System.Security.Claims.Claim("PatientID", patientId.Value.ToString()));
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

            claims.Add(new System.Security.Claims.Claim("AdminID", adminId.Value.ToString()));
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
    public async Task<IActionResult> ChangePassword(string? currentPassword, string? password, string? confirmPassword,
        bool returnToProfile = false, bool profilePasswordRequest = false)
    {
        bool isProfileAjaxRequest = profilePasswordRequest &&
            string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);
        IActionResult ProfilePasswordError()
        {
            var message = ModelState.Values.SelectMany(value => value.Errors)
                .Select(error => error.ErrorMessage).FirstOrDefault() ?? "Your password could not be changed.";
            return isProfileAjaxRequest ? BadRequest(new { message }) : View();
        }

        ValidateNewPassword(password, confirmPassword);
        if (string.IsNullOrEmpty(currentPassword) || currentPassword.Length > 128)
            ModelState.AddModelError("", "Enter your current password.");
        if (!ModelState.IsValid) return isProfileAjaxRequest ? ProfilePasswordError() : returnToProfile ? await Profile() : View();
        var user = await repository.GetActiveUserById(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
        if (user == null || !PasswordHelper.VerifyPassword(currentPassword!, user.PasswordHash, user.PasswordSalt))
        {
            ModelState.AddModelError("", "Your current password is incorrect.");
            return isProfileAjaxRequest ? ProfilePasswordError() : returnToProfile ? await Profile() : View();
        }
        if (PasswordHelper.VerifyPassword(password!, user.PasswordHash, user.PasswordSalt))
        {
            ModelState.AddModelError("", "Choose a password different from your current password.");
            return isProfileAjaxRequest ? ProfilePasswordError() : returnToProfile ? await Profile() : View();
        }
        var salt = PasswordHelper.GenerateSalt();
        if (!await repository.ChangePassword(user.UserId, user.PasswordHash, PasswordHelper.HashPassword(password!, salt), salt))
        {
            ModelState.AddModelError("", "Your account changed. Sign in again and retry.");
            return isProfileAjaxRequest ? ProfilePasswordError() : returnToProfile ? await Profile() : View();
        }
        await HttpContext.SignOutAsync();
        TempData["AccountMessage"] = "Your password has been changed. Sign in again.";
        if (isProfileAjaxRequest)
            return Ok(new { redirectUrl = Url.Action(nameof(Login), "Account") });
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
        ViewBag.Provinces = await repository.GetProvinces();
        ViewBag.StructuredAddress = await repository.GetUserAddress(user.UserId);
        return View(user);
    }

    [Authorize, HttpGet]
    public async Task<IActionResult> AddressTownCities(int provinceId) =>
        provinceId <= 0 ? Json(Array.Empty<object>()) : Json(await repository.GetTownCities(provinceId));

    [Authorize, HttpGet]
    public async Task<IActionResult> AddressSuburbs(int townCityId) =>
        townCityId <= 0 ? Json(Array.Empty<object>()) : Json(await repository.GetSuburbs(townCityId));

    [Authorize, HttpPost, ValidateAntiForgeryToken, EnableRateLimiting("account")]
    public async Task<IActionResult> Profile(string? name, string? surname, string? email,
        string? cellphone, string? addressLine1, string? addressLine2, int? suburbId, string? currentPassword)
    {
        var user = await repository.GetActiveUserById(int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
        if (user == null) return Challenge();
        name = name?.Trim();
        surname = surname?.Trim();
        email = email?.Trim();
        cellphone = cellphone?.Trim();
        addressLine1 = addressLine1?.Trim();
        addressLine2 = addressLine2?.Trim();
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
        if (string.IsNullOrWhiteSpace(addressLine1) || addressLine1.Length > 150)
            ModelState.AddModelError("", "Enter address line 1 (up to 150 characters).");
        if (addressLine2?.Length > 150) ModelState.AddModelError("", "Address line 2 must be 150 characters or fewer.");
        if (!suburbId.HasValue || suburbId <= 0) ModelState.AddModelError("", "Select a suburb.");
        var emailChanged = !string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase);
        if (emailChanged && (string.IsNullOrEmpty(currentPassword) || currentPassword.Length > 128 ||
            !PasswordHelper.VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt)))
            ModelState.AddModelError("", "Enter your current password to change your email address / username.");
        if (ModelState.IsValid)
        {
            try
            {
                var existingAddress = await repository.GetUserAddress(user.UserId);
                var normalizedAddressLine2 = string.IsNullOrWhiteSpace(addressLine2) ? null : addressLine2;
                var updatedFields = new List<string>();

                if (!string.Equals(user.Name, name, StringComparison.Ordinal))
                    updatedFields.Add("First name");
                if (!string.Equals(user.Surname, surname, StringComparison.Ordinal))
                    updatedFields.Add("Surname");
                if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
                    updatedFields.Add("Email address");
                if (!string.Equals(user.Cellphone ?? string.Empty, cellphone ?? string.Empty, StringComparison.Ordinal))
                    updatedFields.Add("Cellphone number");
                if (existingAddress == null ||
                    !string.Equals(existingAddress.AddressLine1, addressLine1, StringComparison.Ordinal) ||
                    !string.Equals(existingAddress.AddressLine2, normalizedAddressLine2, StringComparison.Ordinal) ||
                    existingAddress.SuburbId != suburbId)
                    updatedFields.Add("Residential address");

                if (updatedFields.Count == 0)
                {
                    TempData["ProfileMessage"] = "No profile changes were made.";
                    return RedirectToAction(nameof(Profile));
                }

                if (await repository.UpdateProfileWithAddress(user.UserId, user.SecurityStamp, name!, surname!, email!,
                    cellphone, addressLine1!, normalizedAddressLine2, suburbId!.Value))
                {
                    var updatedState = await repository.GetState(user.UserId);
                    if (updatedState == null)
                    {
                        return Challenge();
                    }

                    var refreshedClaims = new List<Claim>
                    {
                        new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                        new(ClaimTypes.Name, $"{name} {surname}".Trim()),
                        new(ClaimTypes.Email, email!),
                        new(ClaimTypes.Role, updatedState.Role),
                        new("MustChangePassword", updatedState.MustChangePassword.ToString()),
                        new("SecurityStamp", updatedState.SecurityStamp.ToString())
                    };

                    // The user's role cannot change through profile editing. Retain
                    // their existing role-specific identifier claim while renewing
                    // the security stamp in the authentication cookie.
                    foreach (var claimType in new[] { "PatientID", "AdminID" })
                    {
                        var existingClaim = User.FindFirst(claimType);
                        if (existingClaim != null)
                        {
                            refreshedClaims.Add(new Claim(claimType, existingClaim.Value));
                        }
                    }

                    var currentTicket = await HttpContext.AuthenticateAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme);
                    var properties = currentTicket.Properties ?? new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                    };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(new ClaimsIdentity(refreshedClaims,
                            CookieAuthenticationDefaults.AuthenticationScheme)),
                        properties);

                    TempData["ProfileMessage"] = FormatProfileUpdateMessage(updatedFields);
                    return RedirectToAction(nameof(Profile));
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
        user.HomeAddress = addressLine1 ?? "";
        ViewBag.Provinces = await repository.GetProvinces();
        ViewBag.StructuredAddress = await repository.GetUserAddress(user.UserId);
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

    private static string FormatProfileUpdateMessage(IReadOnlyList<string> updatedFields) =>
        updatedFields.Count == 1
            ? $"{updatedFields[0]} updated."
            : $"{string.Join(", ", updatedFields.Take(updatedFields.Count - 1))} and {updatedFields[^1]} updated.";

    private static bool ValidToken(string? token) => token is { Length: 64 } && token.All(Uri.IsHexDigit);
    private static string TokenHash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}


