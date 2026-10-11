using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

public sealed class SouthAfricanIdAttribute : ValidationAttribute
{
    public SouthAfricanIdAttribute() : base("Enter a valid 13-digit South African ID number.") { }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null || value is string { Length: 0 }) return ValidationResult.Success;
        if (value is not string id || !Regex.IsMatch(id, @"\A[0-9]{10}[01][0-9]{2}\z"))
            return new ValidationResult(ErrorMessage);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var year = today.Year / 100 * 100 + int.Parse(id[..2], CultureInfo.InvariantCulture);
        var month = int.Parse(id.Substring(2, 2), CultureInfo.InvariantCulture);
        var day = int.Parse(id.Substring(4, 2), CultureInfo.InvariantCulture);
        if (year > today.Year || (year == today.Year &&
            (month > today.Month || (month == today.Month && day > today.Day)))) year -= 100;
        if (month < 1 || month > 12 || day < 1 || day > DateTime.DaysInMonth(year, month))
            return new ValidationResult(ErrorMessage);

        var sum = 0;
        for (var i = 0; i < id.Length; i++)
        {
            var digit = id[i] - '0';
            if (i % 2 == 1) digit *= 2;
            sum += digit > 9 ? digit - 9 : digit;
        }
        if (sum % 10 != 0) return new ValidationResult(ErrorMessage);

        if (validationContext.ObjectInstance is PatientRegistrationViewModel model &&
            model.DateOfBirth.HasValue && model.DateOfBirth.Value != new DateOnly(year, month, day))
            return new ValidationResult("Date of birth must match the South African ID number.");
        return ValidationResult.Success;
    }
}
