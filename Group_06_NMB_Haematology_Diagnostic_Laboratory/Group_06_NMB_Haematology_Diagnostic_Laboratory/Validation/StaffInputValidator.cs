using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Validation;

public static class StaffInputValidator
{
    public static Dictionary<string, string[]> Validate(User? user, string? hpcsa = null, string? employee = null,
        bool doctor = false, bool technician = false)
    {
        var errors = new Dictionary<string, string[]>();
        void Error(string key, string message) => errors[key] = new[] { message };
        if (user == null) { Error("", "Enter the staff member's details."); return errors; }
        user.Name = user.Name?.Trim() ?? "";
        user.Surname = user.Surname?.Trim() ?? "";
        user.Email = user.Email?.Trim() ?? "";
        user.Cellphone = user.Cellphone?.Trim() ?? "";
        user.SaIdNumber = user.SaIdNumber?.Trim() ?? "";
        user.HomeAddress = user.HomeAddress?.Trim() ?? "";
        foreach (var item in new[] { ("User.Name", user.Name, "First name"), ("User.Surname", user.Surname, "Surname") })
        {
            if (item.Item2.Length == 0) Error(item.Item1, item.Item3 + " is required.");
            else if (item.Item2.Length > 100) Error(item.Item1, item.Item3 + " must be 100 characters or fewer.");
            else if (!Regex.IsMatch(item.Item2, @"^[\p{L}\p{M} .'\u2019-]+$"))
                Error(item.Item1, item.Item3 + " may contain letters, spaces, apostrophes, full stops and hyphens.");
        }
        if (user.Email.Length == 0) Error("User.Email", "Email address is required.");
        else if (user.Email.Length > 150 || !new EmailAddressAttribute().IsValid(user.Email))
            Error("User.Email", "Enter a valid email address, up to 150 characters.");
        if (!Regex.IsMatch(user.Cellphone, @"^[0-9]{10}$"))
            Error("User.Cellphone", "Cellphone number must contain exactly 10 digits (0–9), with no spaces or letters.");
        if (!Regex.IsMatch(user.SaIdNumber, @"^[0-9]{13}$"))
            Error("User.SaIdNumber", "SA ID number must contain exactly 13 digits (0–9), with no spaces or letters.");
        if (!user.DateOfBirth.HasValue) Error("User.DateOfBirth", "Date of birth is required.");
        else if (user.DateOfBirth.Value.Date > DateTime.Today) Error("User.DateOfBirth", "Date of birth cannot be in the future.");
        else if (!errors.ContainsKey("User.SaIdNumber") && user.SaIdNumber[..6] != user.DateOfBirth.Value.ToString("yyMMdd"))
            Error("User.SaIdNumber", "The first 6 ID digits must match the date of birth (YYMMDD).");
        if (user.HomeAddress.Length > 255) Error("User.HomeAddress", "Home address must be 255 characters or fewer.");
        if (doctor && (string.IsNullOrWhiteSpace(hpcsa) || hpcsa.Trim().Length > 20))
            Error("HpcsaNumber", "HPCSA number is required and must be 20 characters or fewer.");
        if (technician && (string.IsNullOrWhiteSpace(employee) || employee.Trim().Length > 50))
            Error("EmployeeNumber", "Employee number is required and must be 50 characters or fewer.");
        return errors;
    }
}
