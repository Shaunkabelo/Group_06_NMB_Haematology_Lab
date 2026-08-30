namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

public class AdminPasswordChange
{
    public string Email { get; set; } = string.Empty;

    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;

    public string ConfirmPassword { get; set; } = string.Empty;
}

public class PasswordCredential
{
    public string PasswordHash { get; set; } = string.Empty;

    public string PasswordSalt { get; set; } = string.Empty;
}
