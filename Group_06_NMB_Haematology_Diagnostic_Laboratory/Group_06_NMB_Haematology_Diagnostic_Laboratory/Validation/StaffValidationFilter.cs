using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Data.SqlClient;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Validation;

public class StaffValidationFilter(IUrlHelperFactory urlHelpers, Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository.ILabManagerRepository repository) : IAsyncActionFilter
{
    private static readonly HashSet<string> Actions = new() {
        "CreateSupplier", "EditSupplier", "CreateConsumable", "EditConsumable", "CreateSampleType", "EditSampleType", "CreateTestCategory", "EditTestCategory", "CreateTestType", "EditTestType", "CreateDoctor", "EditDoctor", "CreateTechnician", "EditTechnician", "CreateManager", "EditManager"
    };

    private static string DuplicateMessage(string action) => action switch
    {
        "CreateDoctor" or "EditDoctor" => "A doctor with this email address, SA ID number or HPCSA number already exists.",
        "CreateTechnician" or "EditTechnician" => "A technician with this email address, SA ID number or employee number already exists.",
        "CreateManager" or "EditManager" => "A lab manager with this email address or SA ID number already exists.",
        "CreateSupplier" or "EditSupplier" => "A supplier with this name or email address already exists.",
        "CreateConsumable" or "EditConsumable" => "A consumable with this name already exists for the selected supplier.",
        "CreateSampleType" or "EditSampleType" => "A sample type with this name already exists.",
        "CreateTestCategory" or "EditTestCategory" => "A test category with this name already exists.",
        "CreateTestType" or "EditTestType" => "A test type with this name already exists.",
        _ => "An item with these details already exists. Use different details and try again."
    };

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var action = context.RouteData.Values["action"]?.ToString() ?? "";
        if (!HttpMethods.IsPost(context.HttpContext.Request.Method) || !Actions.Contains(action))
        { await next(); return; }
        var rangeErrors = new Dictionary<string, string[]>();
        var test = context.ActionArguments.Values.OfType<TestType>().FirstOrDefault();
        if (test != null)
        {
            var form = await context.HttpContext.Request.ReadFormAsync();
            foreach (var field in new[] { "NormalRangeMin", "NormalRangeMax" })
            {
                context.ModelState.Remove(field);
                if (NormalRangeParser.TryParse(form[field].ToString(), out var number))
                {
                    if (field == "NormalRangeMin") test.NormalRangeMin = number;
                    else test.NormalRangeMax = number;
                }
                else rangeErrors[field] = new[] { string.IsNullOrWhiteSpace(form[field]) ? "Enter the normal range value." : NormalRangeParser.Error };
            }
        }
        var doctor = context.ActionArguments.Values.OfType<Doctor>().FirstOrDefault();
        var technician = context.ActionArguments.Values.OfType<LabTechnician>().FirstOrDefault();
        var manager = context.ActionArguments.Values.OfType<LabManager>().FirstOrDefault();
        if (doctor != null) doctor.HpcsaNumber = doctor.HpcsaNumber?.Trim() ?? "";
        if (technician != null) technician.EmployeeNumber = technician.EmployeeNumber?.Trim() ?? "";
        var errors = doctor == null && technician == null && manager == null ? CatalogueInputValidator.Validate(context.ActionArguments.Values) : StaffInputValidator.Validate(doctor?.User ?? technician?.User ?? manager?.User,
            doctor?.HpcsaNumber, technician?.EmployeeNumber, doctor != null, technician != null);
        foreach (var error in rangeErrors) errors[error.Key] = error.Value;
        foreach (var entry in context.ModelState.Where(e => e.Value?.Errors.Count > 0))
            if (!errors.ContainsKey(entry.Key)) errors[entry.Key] = new[] { "Enter a valid value for this field." };
        if (test != null)
        {
            if (test.TestCategoryId > 0 && (await repository.GetTestCategoryById(test.TestCategoryId))?.IsActive != true)
                errors["TestCategoryId"] = new[] { "Choose an active test category." };
            if (test.SampleTypeId > 0 && (await repository.GetSampleTypeById(test.SampleTypeId))?.IsActive != true)
                errors["SampleTypeId"] = new[] { "Choose an active sample type." };
        }
        var consumable = context.ActionArguments.Values.OfType<Consumable>().FirstOrDefault();
        if (consumable?.SupplierId > 0 && (await repository.GetSupplierById(consumable.SupplierId))?.IsActive != true)
            errors["SupplierId"] = new[] { "Choose an active supplier." };
        if (errors.Count > 0)
        { context.Result = new UnprocessableEntityObjectResult(new { errors }); return; }

        var executed = await next();
        if (executed.Exception is SqlException sql && sql.Number is 2601 or 2627)
        {
            executed.ExceptionHandled = true;
            executed.Result = new UnprocessableEntityObjectResult(new {
                errors = new Dictionary<string, string[]> { [""] = new[] {
                    DuplicateMessage(action)
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


