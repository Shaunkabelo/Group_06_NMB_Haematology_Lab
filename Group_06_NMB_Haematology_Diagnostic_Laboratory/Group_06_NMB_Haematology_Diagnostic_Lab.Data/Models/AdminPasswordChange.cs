using System.ComponentModel.DataAnnotations;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

public class UserPasswordChange
{
    [Required, StringLength(255)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(255)]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$", ErrorMessage = "Password must contain at least 8 characters, one uppercase letter, one number, and one special character.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required, StringLength(255)]
    [Compare(nameof(NewPassword), ErrorMessage = "The new password and confirmation do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class AdminPasswordChange : UserPasswordChange
{
    public string Email { get; set; } = string.Empty;
}

public class PasswordCredential
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
}

public class PasswordChangeResult
{
    public int Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
