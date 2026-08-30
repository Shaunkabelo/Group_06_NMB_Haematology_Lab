namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

/// <summary>
/// Represents a row returned by dbo.sp_GetAllUsers.
/// </summary>
public class UserListItem
{
    public string Name { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool IsActive { get; set; }
}
