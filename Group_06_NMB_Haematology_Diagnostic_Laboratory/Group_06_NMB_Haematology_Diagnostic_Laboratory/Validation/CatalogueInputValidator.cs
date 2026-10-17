using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Validation;

public static class CatalogueInputValidator
{
    public static Dictionary<string, string[]> Validate(IEnumerable<object?> arguments)
    {
        var errors = new Dictionary<string, string[]>();

        foreach (var argument in arguments)
        {
            switch (argument)
            {
                case Supplier supplier:
                    ValidateText(supplier.SupplierName, "SupplierName", "Supplier name", 100);
                    ValidateText(supplier.ContactPerson, "ContactPerson", "Contact person", 100);
                    if (!string.IsNullOrWhiteSpace(supplier.SupplierEmail) &&
                        (supplier.SupplierEmail.Trim().Length > 150 || !new EmailAddressAttribute().IsValid(supplier.SupplierEmail.Trim())))
                        Add("SupplierEmail", "Enter a valid supplier email address, up to 150 characters.");
                    break;
                case Consumable consumable:
                    ValidateText(consumable.Name, "Name", "Consumable name", 100);
                    if (consumable.SupplierId <= 0) Add("SupplierId", "Select a valid supplier.");
                    if (consumable.ReorderLevel < 0) Add("ReorderLevel", "Reorder level cannot be negative.");
                    if (consumable.QuantityOnHand < 0) Add("QuantityOnHand", "Quantity on hand cannot be negative.");
                    break;
                case SampleType sampleType:
                    ValidateText(sampleType.SampleTypeName, "SampleTypeName", "Sample type name", 100);
                    break;
                case TestCategory category:
                    ValidateText(category.CategoryName, "CategoryName", "Category name", 100);
                    ValidateOptionalText(category.CategoryDescription, "CategoryDescription", "Category description", 500);
                    break;
                case TestType testType:
                    ValidateText(testType.TestName, "TestName", "Test name", 100);
                    ValidateText(testType.UnitOfMeasurement, "UnitOfMeasurement", "Unit of measurement", 50);
                    if (testType.TestCategoryId <= 0) Add("TestCategoryId", "Select a valid test category.");
                    if (testType.SampleTypeId <= 0) Add("SampleTypeId", "Select a valid sample type.");
                    if (testType.TurnaroundTimeMinutes <= 0) Add("TurnaroundTimeMinutes", "Turnaround time must be greater than zero.");
                    if (testType.NormalRangeMin.HasValue && testType.NormalRangeMax.HasValue && testType.NormalRangeMin > testType.NormalRangeMax)
                        Add("NormalRangeMax", "Maximum normal range must be greater than or equal to the minimum.");
                    break;
            }
        }

        return errors;

        void ValidateText(string? value, string key, string label, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) Add(key, $"{label} is required.");
            else if (value.Trim().Length > maxLength) Add(key, $"{label} must be {maxLength} characters or fewer.");
            else if (!Regex.IsMatch(value.Trim(), @"^[\p{L}\p{M}0-9 .,'’()&/-]+$"))
                Add(key, $"{label} contains unsupported characters.");
        }

        void ValidateOptionalText(string? value, string key, string label, int maxLength)
        {
            if (value?.Trim().Length > maxLength) Add(key, $"{label} must be {maxLength} characters or fewer.");
        }

        void Add(string key, string message) => errors[key] = new[] { message };
    }
}
