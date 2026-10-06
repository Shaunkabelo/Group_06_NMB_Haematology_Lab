using System.ComponentModel.DataAnnotations;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Validation;
public static class CatalogueInputValidator
{
    public static Dictionary<string, string[]> Validate(IEnumerable<object?> arguments)
    {
        var errors = new Dictionary<string, string[]>();
        void Text(string key, string? value, int max, bool required = true) {
            if (required && string.IsNullOrWhiteSpace(value)) errors[key] = new[] { "This field is required." };
            else if (value?.Length > max) errors[key] = new[] { $"Use {max} characters or fewer." };
        }
        void Positive(string key, int value) { if(value <= 0) errors[key] = new[] { "Select a valid option or enter a whole number greater than zero." }; }
        foreach(var argument in arguments) {
            switch(argument) {
                case Supplier s:
                    s.SupplierName = s.SupplierName?.Trim(); s.ContactPerson = s.ContactPerson?.Trim(); s.SupplierEmail = s.SupplierEmail?.Trim();
                    Text("SupplierName", s.SupplierName, 100); Text("ContactPerson", s.ContactPerson, 100); Text("SupplierEmail", s.SupplierEmail, 150);
                    if(!new EmailAddressAttribute().IsValid(s.SupplierEmail)) errors["SupplierEmail"] = new[] { "Enter a valid email address." };
                    break;
                case SampleType s: s.SampleTypeName = s.SampleTypeName?.Trim(); Text("SampleTypeName", s.SampleTypeName, 100); break;
                case TestCategory c: c.CategoryName = c.CategoryName?.Trim(); Text("CategoryName", c.CategoryName, 100); Text("CategoryDescription", c.CategoryDescription, 255, false); break;
                case Consumable c:
                    c.Name = c.Name?.Trim(); Text("Name", c.Name, 100); Positive("SupplierId", c.SupplierId);
                    if(c.QuantityOnHand < 0) errors["QuantityOnHand"] = new[] { "Quantity on hand cannot be negative." };
                    if(c.ReorderLevel < 0) errors["ReorderLevel"] = new[] { "Reorder level cannot be negative." };
                    break;
                case TestType t:
                    t.TestName = t.TestName?.Trim(); t.UnitOfMeasurement = t.UnitOfMeasurement?.Trim();
                    Text("TestName", t.TestName, 100); Text("UnitOfMeasurement", t.UnitOfMeasurement, 50);
                    Positive("TestCategoryId", t.TestCategoryId); Positive("SampleTypeId", t.SampleTypeId); Positive("TurnaroundTimeMinutes", t.TurnaroundTimeMinutes);
                    if (!t.NormalRangeMin.HasValue) errors["NormalRangeMin"] = new[] { "Enter the minimum normal range." };
                    if (!t.NormalRangeMax.HasValue) errors["NormalRangeMax"] = new[] { "Enter the maximum normal range." };
                    if(t.NormalRangeMin > t.NormalRangeMax) errors["NormalRangeMax"] = new[] { "Maximum must be greater than or equal to minimum." };
                    break;
                case IEnumerable<TestTypeConsumable> requirements:
                    var rows = requirements.ToList();
                    if(rows.Count == 0 || rows.Any(r => r.ConsumableId <= 0 || r.QuantityRequired <= 0))
                        errors[""] = new[] { "Select a consumable and a positive whole-number quantity for every requirement." };
                    if(rows.Select(r => r.ConsumableId).Distinct().Count() != rows.Count) errors[""] = new[] { "Select each consumable only once." };
                    break;
            }
        }
        return errors;
    }
}
