using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;

public record AccountDestination(string Controller, string Action);

public class AccountNavigation(IConfiguration configuration, IActionDescriptorCollectionProvider actions)
{
    public AccountDestination Destination(ClaimsPrincipal user) => Destination(user.FindFirstValue(ClaimTypes.Role));
    public AccountDestination Destination(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return new("Account", "Workspace");
        var controller = configuration[$"AccountNavigation:{role}:Controller"];
        var action = configuration[$"AccountNavigation:{role}:Action"];
        if (string.IsNullOrWhiteSpace(controller) || string.IsNullOrWhiteSpace(action))
            (controller, action) = role switch
            {
                "LabManager" => ("LabManager", "Dashboard"),
                "Admin" => ("Admin", "Dashboard"),
                "Doctor" => ("Doctor", "Dashboard"),
                "LabTechnician" => ("LabTechnician", "Dashboard"),
                "Patient" => ("Patient", "Dashboard"),
                _ => ("Account", "Workspace")
            };
        var exists = actions.ActionDescriptors.Items.OfType<ControllerActionDescriptor>().Any(a =>
            string.Equals(a.ControllerName, controller, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(a.ActionName, action, StringComparison.OrdinalIgnoreCase) &&
            (!a.RouteValues.TryGetValue("area", out var area) || string.IsNullOrEmpty(area)) &&
            (a.ActionConstraints?.OfType<HttpMethodActionConstraint>().All(c => c.HttpMethods.Contains("GET")) ?? true));
        return exists ? new(controller!, action!) : new("Account", "Workspace");
    }
}
