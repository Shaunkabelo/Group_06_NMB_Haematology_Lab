namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models
{
    public class PatientMedicalInformation
    {
        public string Category { get; set; } = string.Empty;

        public string SelectedItem { get; set; } = string.Empty;

        public string BadgeClass { get; set; } = string.Empty;

        public string ItemType { get; set; } = string.Empty;
    }

    public class PatientConditionInformationRow
    {
        public string ConditionName { get; set; } = string.Empty;

        public string ConditionCategory { get; set; } = string.Empty;
    }

    public class PatientMedicationInformationRow
    {
        public string MedicationName { get; set; } = string.Empty;

        public string MedicationCategory { get; set; } = string.Empty;
    }

    public class PatientAllergyInformationRow
    {
        public string AllergyName { get; set; } = string.Empty;

        public string AllergyCategory { get; set; } = string.Empty;
    }
}
