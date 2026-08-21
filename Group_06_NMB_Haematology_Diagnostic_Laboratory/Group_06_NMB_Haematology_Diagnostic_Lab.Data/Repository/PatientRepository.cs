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
            var allergies = await _db.GetData<string, object>(
                "dbo.sp_GetAllergiesByCategoryName",
                new { CategoryName = categoryName });

            return allergies
                .Where(allergy => !string.IsNullOrWhiteSpace(allergy))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(allergy => allergy)
                .ToList();
        }

        public async Task<IEnumerable<string>> GetMedicalConditionCategories()
        {
            var rows = await _db.GetData<MedicalConditionCategoryOption, object>(
                "dbo.sp_GetMedicalConditionsCategories",
                new { });

            return rows
                .Select(row => row.CategoryName)
                .Where(category => !string.IsNullOrWhiteSpace(category))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(category => category)
                .ToList();
        }

        public async Task<IEnumerable<string>> GetMedicalConditionsByCategory(string categoryName)
        {
            var conditions = await _db.GetData<string, object>(
                "dbo.sp_GetMedicalConditionsByCategory",
                new { CategoryName = categoryName });

            return conditions
                .Where(condition => !string.IsNullOrWhiteSpace(condition))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(condition => condition)
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
            var medications = await _db.GetData<string, object>(
                "dbo.sp_GetMedicationsByCategory",
                new { CategoryName = categoryName });

            return medications
                .Where(medication => !string.IsNullOrWhiteSpace(medication))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(medication => medication)
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
   

   


