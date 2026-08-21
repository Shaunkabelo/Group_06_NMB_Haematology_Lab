namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models
{
    /// <summary>
    /// Represents a row returned by dbo.sp_GetMedicalConditionsCategories.
    /// </summary>
    public class MedicalConditionCategoryOption
    {
        public string CategoryName { get; set; } = string.Empty;
    }
}
