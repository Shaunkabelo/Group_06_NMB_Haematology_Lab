namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

public class AuthenticationState
{
    public int UserId { get; set; }
    public Guid SecurityStamp { get; set; }
    public bool MustChangePassword { get; set; }
    public string Role { get; set; } = string.Empty;
}

public partial class User
{
    public Guid SecurityStamp { get; set; }
}
