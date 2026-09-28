using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Data.SqlClient;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Validation;

public class StaffValidationFilter(IUrlHelperFactory urlHelpers) : IAsyncActionFilter
{
    private static readonly HashSet<string> Actions = new() {
        "CreateSupplier", "EditSupplier", "CreateConsumable", "EditConsumable", "CreateSampleType", "EditSampleType", "CreateTestCategory", "EditTestCategory", "CreateTestType", "EditTestType", "CreateDoctor", "EditDoctor", "CreateTechnician", "EditTechnician", "CreateManager", "EditManager"
    };
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!HttpMethods.IsPost(context.HttpContext.Request.Method) ||
            !Actions.Contains(context.RouteData.Values["action"]?.ToString() ?? ""))
        { await next(); return; }
        var doctor = context.ActionArguments.Values.OfType<Doctor>().FirstOrDefault();
        var technician = context.ActionArguments.Values.OfType<LabTechnician>().FirstOrDefault();
        var manager = context.ActionArguments.Values.OfType<LabManager>().FirstOrDefault();
        if (doctor != null) doctor.HpcsaNumber = doctor.HpcsaNumber?.Trim() ?? "";
        if (technician != null) technician.EmployeeNumber = technician.EmployeeNumber?.Trim() ?? "";
        var errors = doctor == null && technician == null && manager == null ? CatalogueInputValidator.Validate(context.ActionArguments.Values) : StaffInputValidator.Validate(doctor?.User ?? technician?.User ?? manager?.User,
            doctor?.HpcsaNumber, technician?.EmployeeNumber, doctor != null, technician != null);
        foreach (var entry in context.ModelState.Where(e => e.Value?.Errors.Count > 0))
            if (!errors.ContainsKey(entry.Key)) errors[entry.Key] = new[] { "Enter a valid value for this field." };
        if (errors.Count > 0)
        { context.Result = new UnprocessableEntityObjectResult(new { errors }); return; }

        var executed = await next();
        if (executed.Exception is SqlException sql && sql.Number is 2601 or 2627)
        {
            executed.ExceptionHandled = true;
            executed.Result = new UnprocessableEntityObjectResult(new {
                errors = new Dictionary<string, string[]> { [""] = new[] {
                    "These details already exist. Check the name, email address, SA ID or registration/employee number and use a unique value."
                }}
            });
        }
        if (context.HttpContext.Request.Headers["X-Requested-With"] == "StaffForm" &&
            executed.Result is RedirectToActionResult redirect)
        {
            executed.Result = new JsonResult(new { redirectUrl = urlHelpers.GetUrlHelper(context)
                .Action(redirect.ActionName, redirect.ControllerName, redirect.RouteValues) });
        }
    }
}


