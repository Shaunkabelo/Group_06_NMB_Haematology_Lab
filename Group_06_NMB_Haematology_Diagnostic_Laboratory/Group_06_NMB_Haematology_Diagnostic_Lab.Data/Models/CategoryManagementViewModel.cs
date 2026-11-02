namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

public class CategoryManagementViewModel
{
    public IEnumerable<CategoryItem> AllergyCategories { get; set; } = [];

    public IEnumerable<CategoryItem> MedicationCategories { get; set; } = [];

    public IEnumerable<CategoryItem> MedicalConditionCategories { get; set; } = [];
}

public class CategoryItem
{
    public bool IsActive { get; set; } = true;
    public string CategoryName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}

public class CategorySave
{
    public string CategoryType { get; set; } = string.Empty;

    public string OriginalCategoryName { get; set; } = string.Empty;

    public string CategoryName { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
