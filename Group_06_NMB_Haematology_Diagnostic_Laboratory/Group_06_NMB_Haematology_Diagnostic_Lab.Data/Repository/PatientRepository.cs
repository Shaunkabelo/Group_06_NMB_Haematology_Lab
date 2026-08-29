using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;
using System;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
        public class PatientRepository : IPatientRepository
        {
            private readonly ISqlDataAccess _db;

        private readonly IPasswordService _pass;

        public PatientRepository(ISqlDataAccess db, IPasswordService pass)
        {
                    _db = db;
            
    
                _db = db;
                _pass = pass;
            }
        

            public async Task<PatientRegistrationResult> RegisterPatient(
                PatientRegistrationViewModel model)
            {
            var passwordResult =
        _pass.HashPassword(model.Password);

            var parameters = new
                {
                    Name = model.Name,
                    Surname = model.Surname,
                    SaIdNumber = model.SaIdNumber,

                    DateOfBirth = model.DateOfBirth.HasValue
            ? model.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue)
            : (DateTime?)null,

                    Cellphone = model.Cellphone,
                    Email = model.Email,
                    HomeAddress = model.HomeAddress,

                     PasswordHash = passwordResult.Hash,
                    PasswordSalt = passwordResult.Salt
                };

                return await _db.SaveDataReturnId<
                    PatientRegistrationResult,
                    object>(
                        "sp_RegisterPatient",
                        parameters);
            }

            public Task RegisterPatient(User user)
            {
                throw new NotImplementedException();
            }

        //==========================================================================================
        //Get patient for update
        //==========================================================================================
        public async Task<PatientUpdate> GetPatientForUpdate(
           int patientId)
        {
            var result = await _db.GetData<
                PatientUpdate,
                object>(
                    "dbo.sp_GetPatientForUpdate",
                    new
                    {
                        PatientId = patientId
                    });

            return result.FirstOrDefault();
        }


        //==========================================================================================
        //Update patient information
        //==========================================================================================

        public async Task UpdatePatient(
        PatientUpdate model)
        {
            var parameters = new
            {
                UserId = model.UserId,
                Name = model.Name,
                Surname = model.Surname,
                Email = model.Email,
                Cellphone = model.Cellphone,
                HomeAddress = model.HomeAddress,
                DateOfBirth = model.DateOfBirth
            };

            await _db.SaveData(
                "dbo.sp_UpdatePatient",
                parameters);
        }

        public async Task<IEnumerable<string>> GetAllergyCategories()
        {
            var rows = await _db.GetData<AllergyCategoryOption, object>(
                "dbo.sp_GetAllergyCategories",
                new { });

            return rows
                .Select(row => row.CategoryName)
                .Where(category => !string.IsNullOrWhiteSpace(category))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(category => category)
                .ToList();
        }

        public async Task<IEnumerable<string>> GetAllergiesByCategoryName(string categoryName)
        {
            var allergies = await _db.GetData<AllergyOption, object>(
                "dbo.sp_GetAllAllergies",
                new { });

            return allergies
                .Where(allergy => string.Equals(
                    allergy.AllergyCategory,
                    categoryName,
                    StringComparison.OrdinalIgnoreCase))
                .Select(allergy => allergy.AllergyName)
                .Where(allergyName => !string.IsNullOrWhiteSpace(allergyName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(allergyName => allergyName)
                .ToList();
        }

        public async Task<int> GetAllergyCount()
        {
            var allergies = await _db.GetData<AllergyOption, object>(
                "dbo.sp_GetAllAllergiesByCategory",
                new { });

            return allergies.Count();
        }

        public async Task<IEnumerable<AllergyOption>> GetAllAllergiesByCategory()
        {
            return await _db.GetData<AllergyOption, object>(
                "dbo.sp_GetAllAllergiesByCategory",
                new { });
        }

        public async Task UpdateAllergy(AllergyUpdate model)
        {
            const string allergyIdQuery = "SELECT AllergyID FROM dbo.Allergy WHERE AllergyName = @AllergyName;";
            const string categoryIdQuery = "SELECT AllergyCategoryID FROM dbo.AllergyCategory WHERE CategoryName = @CategoryName;";

            var allergyId = await _db.QueryFirstOrDefaultAsync<int?>(
                allergyIdQuery,
                new { AllergyName = model.OriginalAllergyName });
            var allergyCategoryId = await _db.QueryFirstOrDefaultAsync<int?>(
                categoryIdQuery,
                new { model.CategoryName });

            if (!allergyId.HasValue)
            {
                throw new InvalidOperationException("Allergy not found.");
            }

            if (!allergyCategoryId.HasValue)
            {
                throw new InvalidOperationException("Allergy category not found.");
            }

            await _db.SaveData(
                "dbo.sp_UpdateAllergy",
                new
                {
                    AllergyID = allergyId.Value,
                    model.AllergyName,
                    AllergyCategoryID = allergyCategoryId.Value,
                    model.AllergyDescription
                });
        }

        public async Task AddAllergy(AllergyCreate model)
        {
            const string categoryIdQuery = "SELECT AllergyCategoryID FROM dbo.AllergyCategory WHERE CategoryName = @CategoryName;";

            var allergyCategoryId = await _db.QueryFirstOrDefaultAsync<int?>(
                categoryIdQuery,
                new { model.CategoryName });

            if (!allergyCategoryId.HasValue)
            {
                throw new InvalidOperationException("Allergy category not found.");
            }

            await _db.SaveData(
                "dbo.sp_AddAllergy",
                new
                {
                    model.AllergyName,
                    AllergyCategoryID = allergyCategoryId.Value,
                    model.AllergyDescription
                });
        }

        public async Task<IEnumerable<string>> GetMedicalConditionCategories()
        {
            var rows = await _db.GetData<MedicalConditionCategoryOption, object>(
                "dbo.sp_GetMedicalConditionCategories",
                new { });

            return rows
                .Select(row => row.CategoryName)
                .Where(category => !string.IsNullOrWhiteSpace(category))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(category => category)
                .ToList();
        }

        public async Task<IEnumerable<MedicalConditionWithPatientCount>> GetMedicalConditionsWithPatientCount()
        {
            var conditions = (await _db.GetData<MedicalConditionWithPatientCount, object>(
                "dbo.sp_GetMedicalConditionsWithPatientCount",
                new { })).ToList();

            var activeConditionNames = (await _db.GetData<MedicalConditionOption, object>(
                "dbo.sp_GetAllMedicalConditions",
                new { }))
                .Select(condition => condition.ConditionName)
                .Where(conditionName => !string.IsNullOrWhiteSpace(conditionName))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var condition in conditions)
            {
                condition.IsActive = activeConditionNames.Contains(condition.ConditionName);
            }

            return conditions;
        }

        public async Task<int> GetMedicalConditionCount()
        {
            var conditions = await _db.GetData<MedicalConditionWithPatientCount, object>(
                "dbo.sp_GetMedicalConditionsWithPatientCount",
                new { });

            return conditions.Count();
        }

        public Task UpdateMedicalCondition(MedicalConditionUpdate model)
        {
            return _db.SaveData(
                "dbo.sp_UpdateMedicalConditions",
                new
                {
                    model.OldConditionName,
                    model.ConditionName,
                    model.CategoryName,
                    model.CategoryDescription,
                    model.MedicalConditionDescription
                });
        }

        public Task AddMedicalCondition(MedicalConditionCreate model)
        {
            return _db.SaveData(
                "dbo.sp_AddMedicalCondition",
                new
                {
                    model.ConditionName,
                    model.CategoryName,
                    model.CategoryDescription
                });
        }

        public Task SetMedicalConditionStatus(MedicalConditionStatusUpdate model)
        {
            return _db.SaveData(
                "dbo.sp_SetMedicalConditionStatus",
                new { model.ConditionName, model.IsActive });
        }

        public async Task<IEnumerable<string>> GetMedicalConditionsByCategory(string categoryName)
        {
            var conditions = await _db.GetData<MedicalConditionOption, object>(
                "dbo.sp_GetAllMedicalConditions",
                new { });

            return conditions
                .Where(condition => string.Equals(
                    condition.ConditionCategory,
                    categoryName,
                    StringComparison.OrdinalIgnoreCase))
                .Select(condition => condition.ConditionName)
                .Where(conditionName => !string.IsNullOrWhiteSpace(conditionName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(conditionName => conditionName)
                .ToList();
        }

        public async Task<IEnumerable<string>> GetMedicationCategories()
        {
            var rows = await _db.GetData<MedicationCategoryOption, object>(
                "dbo.sp_GetMedicationCategories",
                new { });

            return rows
                .Select(row => row.CategoryName)
                .Where(category => !string.IsNullOrWhiteSpace(category))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(category => category)
                .ToList();
        }

        public async Task<IEnumerable<string>> GetMedicationsByCategory(string categoryName)
        {
            var medications = await _db.GetData<MedicationOption, object>(
                "dbo.sp_GetAllMedications",
                new { });

            return medications
                .Where(medication => string.Equals(
                    medication.MedicationCategory?.Trim(),
                    categoryName.Trim(),
                    StringComparison.OrdinalIgnoreCase))
                .Select(medication => medication.MedicationName?.Trim())
                .Where(medicationName => !string.IsNullOrWhiteSpace(medicationName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(medicationName => medicationName)
                .ToList();
        }

        public async Task SavePatientMedication(int patientId, string medicationName)
        {
            const string medicationIdQuery = """
                SELECT MedicationID
                FROM dbo.Medication
                WHERE MedicationName = @MedicationName;
                """;

            var medicationId = await _db.QueryFirstOrDefaultAsync<int?>(
                medicationIdQuery,
                new { MedicationName = medicationName });

            if (!medicationId.HasValue)
            {
                throw new InvalidOperationException("The selected medication could not be found.");
            }

            await _db.SaveData(
                "dbo.sp_SavePatientMedication",
                new
                {
                    PatientID = patientId,
                    MedicationID = medicationId.Value
                });
        }

        public async Task SavePatientAllergy(int patientId, string allergyName)
        {
            const string allergyIdQuery = """
                SELECT AllergyID
                FROM dbo.Allergy
                WHERE AllergyName = @AllergyName;
                """;

            var allergyId = await _db.QueryFirstOrDefaultAsync<int?>(
                allergyIdQuery,
                new { AllergyName = allergyName });

            if (!allergyId.HasValue)
            {
                throw new InvalidOperationException("The selected allergy could not be found.");
            }

            await _db.SaveData(
                "dbo.sp_SavePatientAllergy",
                new
                {
                    PatientID = patientId,
                    AllergyID = allergyId.Value
                });
        }

        public async Task SavePatientMedicalCondition(int patientId, string conditionName)
        {
            const string medicalConditionIdQuery = """
                SELECT MedicalConditionID
                FROM dbo.MedicalCondition
                WHERE ConditionName = @ConditionName;
                """;

            var medicalConditionId = await _db.QueryFirstOrDefaultAsync<int?>(
                medicalConditionIdQuery,
                new { ConditionName = conditionName });

            if (!medicalConditionId.HasValue)
            {
                throw new InvalidOperationException("The selected medical condition could not be found.");
            }

            await _db.SaveData(
                "dbo.sp_SavePatientMedicalCondition",
                new
                {
                    PatientID = patientId,
                    MedicalConditionID = medicalConditionId.Value
                });
        }

        public async Task<IEnumerable<PatientMedicalInformation>> GetPatientMedicalInformation(int patientId)
        {
            var results = await _db.GetMultipleData<
                PatientConditionInformationRow,
                PatientMedicationInformationRow,
                PatientAllergyInformationRow,
                object>(
                    "dbo.sp_GetPatientMedicalInformation",
                    new { PatientID = patientId });

            return results.First
                .Select(row => new PatientMedicalInformation
                {
                    Category = row.ConditionCategory,
                    SelectedItem = row.ConditionName,
                    BadgeClass = "badge-amber",
                    ItemType = "Condition"
                })
                .Concat(results.Second.Select(row => new PatientMedicalInformation
                {
                    Category = row.MedicationCategory,
                    SelectedItem = row.MedicationName,
                    BadgeClass = "badge-teal",
                    ItemType = "Medication"
                }))
                .Concat(results.Third.Select(row => new PatientMedicalInformation
                {
                    Category = row.AllergyCategory,
                    SelectedItem = row.AllergyName,
                    BadgeClass = "badge-red",
                    ItemType = "Allergy"
                }))
                .Where(item => !string.IsNullOrWhiteSpace(item.SelectedItem))
                .OrderBy(item => item.Category)
                .ThenBy(item => item.SelectedItem)
                .ToList();
        }

        public async Task DeletePatientMedicalCondition(int patientId, string conditionName)
        {
            const string medicalConditionIdQuery = """
                SELECT MedicalConditionID
                FROM dbo.MedicalCondition
                WHERE ConditionName = @ConditionName;
                """;

            var medicalConditionId = await _db.QueryFirstOrDefaultAsync<int?>(
                medicalConditionIdQuery,
                new { ConditionName = conditionName });

            if (!medicalConditionId.HasValue)
            {
                throw new InvalidOperationException("The selected medical condition could not be found.");
            }

            await _db.SaveData(
                "dbo.sp_DeletePatientCondition",
                new
                {
                    PatientID = patientId,
                    MedicalConditionID = medicalConditionId.Value
                });
        }

        public async Task DeletePatientAllergy(int patientId, string allergyName)
        {
            const string allergyIdQuery = """
                SELECT AllergyID
                FROM dbo.Allergy
                WHERE AllergyName = @AllergyName;
                """;

            var allergyId = await _db.QueryFirstOrDefaultAsync<int?>(
                allergyIdQuery,
                new { AllergyName = allergyName });

            if (!allergyId.HasValue)
            {
                throw new InvalidOperationException("The selected allergy could not be found.");
            }

            await _db.SaveData(
                "dbo.sp_DeletePatientAllergy",
                new
                {
                    PatientID = patientId,
                    AllergyID = allergyId.Value
                });
        }

        public async Task DeletePatientMedication(int patientId, string medicationName)
        {
            const string medicationIdQuery = """
                SELECT MedicationID
                FROM dbo.Medication
                WHERE MedicationName = @MedicationName;
                """;

            var medicationId = await _db.QueryFirstOrDefaultAsync<int?>(
                medicationIdQuery,
                new { MedicationName = medicationName });

            if (!medicationId.HasValue)
            {
                throw new InvalidOperationException("The selected medication could not be found.");
            }

            await _db.SaveData(
                "dbo.sp_DeletePatientMedication",
                new
                {
                    PatientID = patientId,
                    MedicationID = medicationId.Value
                });
        }
    }
    }
   

   


