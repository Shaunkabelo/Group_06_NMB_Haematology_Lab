using System.Security.Claims;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;

public class AccountCookieEvents(IAuthenticationRepository repository) : CookieAuthenticationEvents
{
    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;
        if (!int.TryParse(principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
            return;
        }
        var state = await repository.GetState(id);
        if (state == null || state.SecurityStamp.ToString() != principal?.FindFirstValue("SecurityStamp") ||
            state.Role != principal.FindFirstValue(ClaimTypes.Role) ||
            state.MustChangePassword.ToString() != principal.FindFirstValue("MustChangePassword"))
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
        }
    }
}
