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
        public async Task<int> GetPatientAbnormalResultCount(int patientId)
        {
            var counts = await _db.LoadData<int, object>(
                """
                SELECT COUNT(DISTINCT r.TestResultID)
                FROM dbo.TestRequest tr
                INNER JOIN dbo.TestRequestTestType t ON t.TestRequestID = tr.TestRequestID
                INNER JOIN dbo.TestType tt ON tt.TestTypeID = t.TestTypeID
                INNER JOIN dbo.LabTest l ON l.TestRequestTestTypeID = t.TestRequestTestTypeID
                INNER JOIN dbo.TestResult r ON r.LabTestID = l.LabTestID
                WHERE tr.PatientID = @PatientID
                  AND LOWER(LTRIM(RTRIM(tr.Status))) IN ('released by doctor', 'released by the doctor')
                  AND (r.IsAbnormal = 1 OR r.ResultValue < tt.NormalRangeMin OR r.ResultValue > tt.NormalRangeMax);
                """, new { PatientID = patientId });
            return counts.Single();
        }
        public async Task<string?> GetPatientDisplayName(int patientId)
        {
            var names = await _db.LoadData<string, object>(
                "SELECT LTRIM(RTRIM(CONCAT(u.Name, ' ', u.Surname))) FROM dbo.Patient p INNER JOIN dbo.[User] u ON u.UserID = p.UserID WHERE p.PatientID = @PatientID;",
                new { PatientID = patientId });
            return names.FirstOrDefault();
        }
        public Task<IEnumerable<SavedPatientConsent>> GetSavedPatientConsents(int patientId) =>
            _db.GetData<SavedPatientConsent, object>("dbo.sp_GetPatientConsents", new { PatientID = patientId });
        public Task RevokePatientConsents(int patientId, int doctorId, int[] testIds) =>
            _db.SaveData("dbo.sp_RevokePatientConsents", new {
                PatientID = patientId, DoctorID = doctorId,
                TestIDs = System.Text.Json.JsonSerializer.Serialize(testIds)
            });
        public Task SavePatientConsents(int patientId, int doctorId, int[] testIds) =>
            _db.SaveData("dbo.sp_SavePatientConsents", new {
                PatientID = patientId, DoctorID = doctorId,
                TestIDs = System.Text.Json.JsonSerializer.Serialize(testIds)
            });

        public Task<IEnumerable<ReleasedConsentRequest>> GetReleasedConsentRequests(int patientId) =>
            _db.GetData<ReleasedConsentRequest, object>(
                "dbo.sp_Patient_GetReleasedTestRequests", new { PatientID = patientId });

        public Task<IEnumerable<PatientRequestDetail>> GetConsentRequestTests(int patientId, int testRequestId) =>
            _db.GetData<PatientRequestDetail, object>(
                "dbo.sp_Patient_GetConsentRequestTests", new { PatientID = patientId, TestRequestID = testRequestId });

        public Task<IEnumerable<PatientRequestDetail>> GetPatientRequestDetails(int patientId, int testRequestId)
        {
            return _db.GetData<PatientRequestDetail, object>(
                "dbo.sp_Patient_GetTestRequestDetails",
                new { PatientID = patientId, TestRequestID = testRequestId });
        }

        public Task<IEnumerable<PatientRequestDetail>> GetPatientRequestProgress(int patientId, int testRequestId)
        {
            return _db.GetData<PatientRequestDetail, object>(
                "dbo.sp_Patient_GetTestRequestProgress",
                new { PatientID = patientId, TestRequestID = testRequestId });
        }

        public Task<IEnumerable<PatientRequest>> GetPatientRequests(int patientId)
        {
            return _db.GetData<PatientRequest, object>(
                "dbo.sp_GetPatientRequests", new { PatientID = patientId });
        }

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
        public async Task<PatientUpdate?> GetPatientForUpdate(
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
            // The admin list must include inactive allergies so they can be reactivated.
            // Patient selection continues to use the existing stored procedure.
            const string sql = """
                SELECT a.*, c.CategoryName AS AllergyCategory, c.CategoryName,
                       (SELECT COUNT(DISTINCT pa.PatientID)
                        FROM dbo.PatientAllergy pa
                        WHERE pa.AllergyID = a.AllergyID) AS TotalPatients
                FROM dbo.Allergy a
                LEFT JOIN dbo.AllergyCategory c ON c.AllergyCategoryID = a.AllergyCategoryID
                ORDER BY c.CategoryName, a.AllergyName;
                """;
            return await _db.LoadData<AllergyOption, object>(sql, new { });
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

        public async Task<IEnumerable<MedicationWithPatientCount>> GetMedicationsWithPatientCount()
        {
            var items = (await _db.GetData<MedicationWithPatientCount, object>(
                "dbo.sp_GetAllMedicationsWithPatientCount",
                new { })).ToList();
            var statuses = (await _db.LoadData<MedicationWithPatientCount, object>(
                "SELECT MedicationName, IsActive FROM dbo.Medication;", new { }))
                .ToDictionary(item => item.MedicationName, item => item.IsActive, StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
                if (statuses.TryGetValue(item.MedicationName, out var isActive))
                    item.IsActive = isActive;
            return items;
        }

        public async Task<int> GetMedicationCount()
        {
            var medications = await GetMedicationsWithPatientCount();
            return medications.Count();
        }

        public Task UpdateMedication(MedicationUpdate model)
        {
            return _db.SaveData(
                "dbo.sp_UpdateMedication",
                new
                {
                    model.OldMedicationName,
                    model.MedicationName,
                    model.CategoryName,
                    model.Description
                });
        }

        public Task AddMedication(MedicationCreate model)
        {
            return _db.SaveData(
                "dbo.sp_AddMedication",
                new
                {
                    model.MedicationName,
                    model.CategoryName,
                    model.Description
                });
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
                .Select(medication => medication.MedicationName.Trim())
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

        public Task SaveCategory(CategorySave model)
        {
            var tableName = GetCategoryTableName(model.CategoryType);
            if (string.IsNullOrWhiteSpace(model.OriginalCategoryName))
            {
                var insertSql = $"INSERT INTO dbo.[{tableName}] (CategoryName, CategoryDescription) VALUES (@CategoryName, @Description);";
                return _db.Save(insertSql, new { model.CategoryName, Description = model.Description ?? string.Empty });
            }

            var updateSql = $"UPDATE dbo.[{tableName}] SET CategoryName = @CategoryName, CategoryDescription = @Description WHERE CategoryName = @OriginalCategoryName;";
            return _db.Save(updateSql, new { model.CategoryName, Description = model.Description ?? string.Empty, model.OriginalCategoryName });
        }

        public Task<IEnumerable<CategoryItem>> GetCategoryDetails(string categoryType)
        {
            var tableName = GetCategoryTableName(categoryType);
            var sql = $"SELECT CategoryName, COALESCE(CategoryDescription, '') AS Description FROM dbo.[{tableName}] ORDER BY CategoryName;";
            return _db.LoadData<CategoryItem, object>(sql, new { });
        }

        public Task DeleteCategory(string categoryType, string categoryName)
        {
            var tableName = GetCategoryTableName(categoryType);
            var sql = $"DELETE FROM dbo.[{tableName}] WHERE CategoryName = @CategoryName;";
            return _db.Save(sql, new { CategoryName = categoryName });
        }

        private static string GetCategoryTableName(string categoryType)
        {
            return categoryType.Trim().ToLowerInvariant() switch
            {
                "allergy" => "AllergyCategory",
                "medication" => "MedicationCategory",
                "condition" => "MedicalConditionsCategory",
                _ => throw new ArgumentException("Invalid category type.", nameof(categoryType))
            };
        }

        public async Task<IEnumerable<UserListItem>> GetAllUsers()
        {
            var users = await _db.GetData<UserListItem, object>(
                "dbo.sp_GetAllUsers",
                new { });

            return users.OrderBy(user => user.Name, StringComparer.OrdinalIgnoreCase);
        }

        public async Task<int> GetUserCount()
        {
            var users = await GetAllUsers();
            return users.Count();
        }

        public async Task<IEnumerable<ConsentDoctorOption>> GetActiveConsentDoctors()
        {
            var doctors = await _db.GetData<ConsentDoctorOption, object>(
                "dbo.sp_GetAllDoctors", new { });
            // Also enforce active status when the installed procedure has no status filter.
            var activeEmails = (await _db.LoadData<string, object>(
                """
                SELECT U.Email
                FROM dbo.Doctor D
                INNER JOIN dbo.[User] U ON D.UserID = U.UserID
                WHERE U.IsActive = 1;
                """, new { })).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return doctors.Where(doctor => activeEmails.Contains(doctor.Email));
        }

        public async Task SetMedicationStatus(string name, int isActive)
        {
            if (string.IsNullOrWhiteSpace(name) || (isActive != 0 && isActive != 1))
                throw new ArgumentException("A name and valid status are required.");
            var affected = await _db.Save(
                "UPDATE dbo.Medication SET IsActive = @IsActive WHERE MedicationName = @Name;",
                new { Name = name, IsActive = isActive });
            if (affected == 0)
                throw new InvalidOperationException("Medication not found.");
        }

        public async Task SetAllergyStatus(string name, int isActive)
        {
            if (string.IsNullOrWhiteSpace(name) || (isActive != 0 && isActive != 1))
                throw new ArgumentException("A name and valid status are required.");
            var affected = await _db.Save(
                "UPDATE dbo.Allergy SET IsActive = @IsActive WHERE AllergyName = @Name;",
                new { Name = name, IsActive = isActive });
            if (affected == 0)
                throw new InvalidOperationException("Allergy not found.");
        }

        public Task SetUserStatus(int userId, int isActive)
        {
            return _db.SaveData(
                "dbo.sp_SetUserStatus",
                new { UserID = userId, IsActive = isActive });
        }

        public Task<UserListItem?> GetAdminProfile(int adminId) =>
            _db.QueryFirstOrDefaultAsync<UserListItem>(
                "SELECT u.UserID, u.Name, u.Surname, u.Email, u.Role, u.CreatedAt, u.IsActive FROM dbo.[User] u INNER JOIN dbo.Admin a ON a.UserID = u.UserID WHERE a.AdminID = @AdminID AND u.Role = 'Admin';",
                new { AdminID = adminId });

        public Task UpdateAdminProfile(int adminId, AdminProfileUpdate model)
        {
            const string sql = """
                UPDATE dbo.[User]
                SET Name = @Name,
                    Surname = @Surname,
                    Email = @Email,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE UserID = (SELECT UserID FROM dbo.Admin WHERE AdminID = @AdminID)
                  AND Role = 'Admin';
                """;
            return _db.Save(sql, new { AdminID = adminId, model.Name, model.Surname, model.Email });
        }

        public async Task ChangeAdminPassword(int adminId, AdminPasswordChange model)
        {
            const string credentialSql = "SELECT PasswordHash, PasswordSalt FROM dbo.[User] WHERE UserID = (SELECT UserID FROM dbo.Admin WHERE AdminID = @AdminID) AND Role = 'Admin';";
            var credential = await _db.QueryFirstOrDefaultAsync<PasswordCredential>(
                credentialSql,
                new { AdminID = adminId });

            if (credential == null)
                throw new InvalidOperationException("Admin account not found.");
            if (!_pass.VerifyPassword(model.CurrentPassword, credential.PasswordHash, credential.PasswordSalt))
                throw new InvalidOperationException("The current password is incorrect.");

            var password = _pass.HashPassword(model.NewPassword);
            const string updateSql = """
                UPDATE dbo.[User]
                SET PasswordHash = @PasswordHash,
                    PasswordSalt = @PasswordSalt,
                    MustChangePassword = 0,
                    UpdatedAt = SYSUTCDATETIME()
                WHERE UserID = (SELECT UserID FROM dbo.Admin WHERE AdminID = @AdminID) AND Role = 'Admin';
                """;
            await _db.Save(updateSql, new
            {
                AdminID = adminId,
                PasswordHash = password.Hash,
                PasswordSalt = password.Salt
            });
        }
    }
    }
   

   


