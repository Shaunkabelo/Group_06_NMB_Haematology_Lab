namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

public partial class User
{
    // Kept outside the generated EF model file. Dapper populates this for Lab Manager staff screens.
    public UserAddress? StructuredAddress { get; set; }
}
