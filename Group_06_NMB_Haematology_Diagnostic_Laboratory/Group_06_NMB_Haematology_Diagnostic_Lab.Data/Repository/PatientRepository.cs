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
        public Task<IEnumerable<ConsentEmailDetail>> RevokePatientConsents(int patientId, int doctorId, int[] testIds)
        {
            return _db.LoadData<ConsentEmailDetail, object>(
                """
                SET NOCOUNT ON;
                DECLARE @Revoked TABLE (TestRequestTestTypeID INT);
                UPDATE dbo.PatientConsent
                SET IsActive = 0
                OUTPUT inserted.TestRequestTestTypeID INTO @Revoked
                WHERE PatientID = @PatientID
                  AND DoctorID = @DoctorID
                  AND TestRequestTestTypeID IN @TestIDs
                  AND IsActive = 1;
                SELECT DISTINCT du.Email AS DoctorEmail,
                    LTRIM(RTRIM(CONCAT(du.Name, ' ', du.Surname))) AS DoctorName,
                    LTRIM(RTRIM(CONCAT(pu.Name, ' ', pu.Surname))) AS PatientName,
                    tt.TestName,
                    'REQ-' + CAST(YEAR(tr.RequestDate) AS VARCHAR(4)) + '-' +
                        RIGHT('00000' + CAST(tr.TestRequestID AS VARCHAR(10)), 5) AS RequestNumber
                FROM @Revoked revoked
                INNER JOIN dbo.TestRequestTestType t ON t.TestRequestTestTypeID = revoked.TestRequestTestTypeID
                INNER JOIN dbo.TestType tt ON tt.TestTypeID = t.TestTypeID
                INNER JOIN dbo.TestRequest tr ON tr.TestRequestID = t.TestRequestID AND tr.PatientID = @PatientID
                INNER JOIN dbo.Patient p ON p.PatientID = tr.PatientID
                INNER JOIN dbo.[User] pu ON pu.UserID = p.UserID
                INNER JOIN dbo.Doctor d ON d.DoctorID = @DoctorID
                INNER JOIN dbo.[User] du ON du.UserID = d.UserID;
                """,
                new { PatientID = patientId, DoctorID = doctorId, TestIDs = testIds.Distinct().ToArray() });
        }
        public Task SavePatientConsents(int patientId, int doctorId, int[] testIds) =>
            _db.SaveData("dbo.sp_SavePatientConsents", new {
                PatientID = patientId, DoctorID = doctorId,
                TestIDs = System.Text.Json.JsonSerializer.Serialize(testIds)
            });

        public Task<IEnumerable<ConsentEmailDetail>> GetConsentEmailDetails(int patientId, int doctorId, int[] testIds) =>
            _db.LoadData<ConsentEmailDetail, object>(
                """
                SELECT du.Email AS DoctorEmail,
                    LTRIM(RTRIM(CONCAT(du.Name, ' ', du.Surname))) AS DoctorName,
                    LTRIM(RTRIM(CONCAT(pu.Name, ' ', pu.Surname))) AS PatientName,
                    tt.TestName,
                    'REQ-' + CAST(YEAR(tr.RequestDate) AS VARCHAR(4)) + '-' +
                        RIGHT('00000' + CAST(tr.TestRequestID AS VARCHAR(10)), 5) AS RequestNumber,
                    MAX(pc.GrantedDate) AS GrantedDate
                FROM dbo.PatientConsent pc
                INNER JOIN dbo.Patient p ON p.PatientID = pc.PatientID
                INNER JOIN dbo.[User] pu ON pu.UserID = p.UserID
                INNER JOIN dbo.Doctor d ON d.DoctorID = pc.DoctorID
                INNER JOIN dbo.[User] du ON du.UserID = d.UserID
                INNER JOIN dbo.TestRequestTestType t ON t.TestRequestTestTypeID = pc.TestRequestTestTypeID
                INNER JOIN dbo.TestType tt ON tt.TestTypeID = t.TestTypeID
                INNER JOIN dbo.TestRequest tr ON tr.TestRequestID = t.TestRequestID
                WHERE pc.PatientID = @PatientID AND tr.PatientID = @PatientID
                    AND pc.DoctorID = @DoctorID AND pc.IsActive = 1 AND du.IsActive = 1
                    AND pc.TestRequestTestTypeID IN @TestIDs
                    AND LOWER(LTRIM(RTRIM(tr.Status))) IN ('released by doctor', 'released by the doctor')
                GROUP BY du.Email, du.Name, du.Surname, pu.Name, pu.Surname,
                    tt.TestName, tr.TestRequestID, tr.RequestDate, t.TestRequestTestTypeID;
                """, new { PatientID = patientId, DoctorID = doctorId, TestIDs = testIds });

        public Task<IEnumerable<ReleasedConsentRequest>> GetReleasedConsentRequests(int patientId) =>
            _db.LoadData<ReleasedConsentRequest, object>(
                """
                SELECT tr.TestRequestID AS RequestID,
                    'REQ-' + CAST(YEAR(tr.RequestDate) AS VARCHAR(4)) + '-' +
                        RIGHT('00000' + CAST(tr.TestRequestID AS VARCHAR(10)), 5) AS TestRequestID,
                    'Dr. ' + u.Name + ' ' + u.Surname AS DoctorName,
                    tr.RequestDate
                FROM dbo.TestRequest tr
                LEFT JOIN dbo.Doctor d ON d.DoctorID = tr.DoctorID
                LEFT JOIN dbo.[User] u ON u.UserID = d.UserID
                WHERE tr.PatientID = @PatientID AND tr.Status = 'Released by doctor'
                ORDER BY tr.RequestDate DESC, tr.TestRequestID DESC;
                """, new { PatientID = patientId });
        public Task<IEnumerable<PatientRequestDetail>> GetConsentRequestTests(int patientId, int testRequestId) =>
            _db.GetData<PatientRequestDetail, object>(
                "dbo.sp_Patient_GetConsentRequestTests", new { PatientID = patientId, TestRequestID = testRequestId });

        public Task<IEnumerable<PatientRequestDetail>> GetPatientRequestDetails(int patientId, int testRequestId)
        {
            return _db.LoadData<PatientRequestDetail, object>(
                """
                SELECT trtt.TestRequestTestTypeID, tt.TestName, trr.ResultValue, trr.Notes AS ResultNotes, tt.UnitOfMeasurement,
                trr.TestResultID, trr.CaptureDateTime, trr.IsAbnormal, lt.CompletionDateTime,
                tr.TestRequestID, tr.RequestDate, tr.DoctorReleaseNote,
                (SELECT LTRIM(RTRIM(CONCAT(u.Name, ' ', u.Surname)))
                 FROM dbo.Doctor d INNER JOIN dbo.[User] u ON u.UserID = d.UserID
                 WHERE d.DoctorID = tr.DoctorID) AS DoctorName,
                (SELECT LTRIM(RTRIM(CONCAT(u.Name, ' ', u.Surname)))
                 FROM dbo.LabTechnician tech INNER JOIN dbo.[User] u ON u.UserID = tech.UserID
                 WHERE tech.LabTechnicianID = COALESCE(trr.CapturedByLabTechnicianID, lt.LabTechnicianID)) AS LabTechnicianName,
                'REQ-' + RIGHT('00000' + CAST(tr.TestRequestID AS VARCHAR(10)), 5) AS RequestNumber,
                (SELECT tc.CategoryName FROM dbo.TestCategory tc WHERE tc.TestCategoryID = tt.TestCategoryID) AS CategoryName,
                CASE
                WHEN tt.NormalRangeMin IS NOT NULL AND tt.NormalRangeMax IS NOT NULL
                THEN CAST(tt.NormalRangeMin AS NVARCHAR(50)) + ' - ' + CAST(tt.NormalRangeMax AS NVARCHAR(50))
                WHEN tt.NormalRangeMin IS NOT NULL
                THEN '>= ' + CAST(tt.NormalRangeMin AS NVARCHAR(50))
                WHEN tt.NormalRangeMax IS NOT NULL
                THEN '<= ' + CAST(tt.NormalRangeMax AS NVARCHAR(50))
                ELSE 'N/A'
                END AS ReferenceRange,
                CASE
                WHEN trr.ResultValue IS NULL THEN 'Pending'
                WHEN tt.NormalRangeMin IS NOT NULL AND trr.ResultValue < tt.NormalRangeMin THEN 'Low'
                WHEN tt.NormalRangeMax IS NOT NULL AND trr.ResultValue > tt.NormalRangeMax THEN 'High'
                ELSE 'Normal'
                END AS ResultFlag
                FROM TestRequest tr
                INNER JOIN TestRequestTestType trtt ON tr.TestRequestID = trtt.TestRequestID
                INNER JOIN TestType tt ON trtt.TestTypeID = tt.TestTypeID
                LEFT JOIN LabTest lt ON trtt.TestRequestTestTypeID = lt.TestRequestTestTypeID
                LEFT JOIN TestResult trr ON lt.LabTestID = trr.LabTestID
                WHERE tr.TestRequestID = @TestRequestID AND tr.PatientID = @PatientID
                AND LOWER(LTRIM(RTRIM(tr.Status))) IN ('released by doctor', 'released by the doctor')
                ORDER BY tt.TestName ASC;
                """,
                new { PatientID = patientId, TestRequestID = testRequestId });
        }

        public Task<IEnumerable<PatientRequestDetail>> GetPatientRequestProgress(int patientId, int testRequestId)
        {
            return _db.LoadData<PatientRequestDetail, object>(
                """
                SELECT tt.TestName, trtt.Status AS TestStatus,
                lt.LabTechnicianID, lt.StartDateTime,
                NULLIF(LTRIM(RTRIM(CONCAT(u.Name, ' ', u.Surname))), '') AS LabTechnicianName
                FROM dbo.TestRequest tr
                INNER JOIN dbo.TestRequestTestType trtt ON tr.TestRequestID = trtt.TestRequestID
                INNER JOIN dbo.TestType tt ON trtt.TestTypeID = tt.TestTypeID
                LEFT JOIN dbo.LabTest lt ON trtt.TestRequestTestTypeID = lt.TestRequestTestTypeID
                LEFT JOIN dbo.LabTechnician tech ON tech.LabTechnicianID = lt.LabTechnicianID
                LEFT JOIN dbo.[User] u ON u.UserID = tech.UserID
                WHERE tr.TestRequestID = @TestRequestID AND tr.PatientID = @PatientID
                ORDER BY tt.TestName ASC;
                """,
                new { PatientID = patientId, TestRequestID = testRequestId });
        }

        public Task<IEnumerable<PatientRequest>> GetPatientRequests(int patientId)
        {
            return _db.GetData<PatientRequest, object>(
                "dbo.sp_GetPatientRequests", new { PatientID = patientId });
        }

        public Task<IEnumerable<PatientTrendResult>> GetPatientTrendResults(int patientId)
        {
            return _db.LoadData<PatientTrendResult, object>(
                """
                SELECT
                    tt.TestTypeID,
                    tt.TestName,
                    (SELECT tc.CategoryName FROM dbo.TestCategory tc WHERE tc.TestCategoryID = tt.TestCategoryID) AS CategoryName,
                    tt.UnitOfMeasurement,
                    tt.NormalRangeMin,
                    tt.NormalRangeMax,
                    CASE
                        WHEN tt.NormalRangeMin IS NOT NULL AND tt.NormalRangeMax IS NOT NULL
                            THEN CAST(tt.NormalRangeMin AS NVARCHAR(50)) + ' - ' + CAST(tt.NormalRangeMax AS NVARCHAR(50))
                        WHEN tt.NormalRangeMin IS NOT NULL
                            THEN '>= ' + CAST(tt.NormalRangeMin AS NVARCHAR(50))
                        WHEN tt.NormalRangeMax IS NOT NULL
                            THEN '<= ' + CAST(tt.NormalRangeMax AS NVARCHAR(50))
                        ELSE 'N/A'
                    END AS ReferenceRange,
                    trr.ResultValue,
                    CASE
                        WHEN tt.NormalRangeMin IS NOT NULL AND trr.ResultValue < tt.NormalRangeMin THEN 'Low'
                        WHEN tt.NormalRangeMax IS NOT NULL AND trr.ResultValue > tt.NormalRangeMax THEN 'High'
                        ELSE 'Normal'
                    END AS ResultFlag,
                    tr.RequestDate AS ResultDate,
                    tr.TestRequestID,
                    'REQ-' + RIGHT('00000' + CAST(tr.TestRequestID AS VARCHAR(10)), 5) AS RequestNumber
                FROM dbo.TestRequest tr
                INNER JOIN dbo.TestRequestTestType trtt ON tr.TestRequestID = trtt.TestRequestID
                INNER JOIN dbo.TestType tt ON trtt.TestTypeID = tt.TestTypeID
                INNER JOIN dbo.LabTest lt ON trtt.TestRequestTestTypeID = lt.TestRequestTestTypeID
                INNER JOIN dbo.TestResult trr ON lt.LabTestID = trr.LabTestID
                WHERE tr.PatientID = @PatientID
                  AND trr.ResultValue IS NOT NULL
                  AND LOWER(LTRIM(RTRIM(tr.Status))) IN ('released by doctor', 'released by the doctor')
                ORDER BY tt.TestName ASC, ResultDate ASC, tr.TestRequestID ASC;
                """,
                new { PatientID = patientId });
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

        public Task<IEnumerable<string>> GetAllergyCategories() =>
            _db.LoadData<string, object>(
                "SELECT CategoryName FROM dbo.[AllergyCategory] WHERE IsActive = 1 ORDER BY CategoryName;",
                new { });
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
            return await _db.ExecuteScalarAsync("SELECT COUNT(*) FROM dbo.Allergy;", new { });
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

        public Task<IEnumerable<string>> GetMedicalConditionCategories() =>
            _db.LoadData<string, object>(
                "SELECT CategoryName FROM dbo.[MedicalConditionsCategory] WHERE IsActive = 1 ORDER BY CategoryName;",
                new { });
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
            return await _db.ExecuteScalarAsync("SELECT COUNT(*) FROM dbo.MedicalCondition;", new { });
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

        public Task<IEnumerable<string>> GetMedicationCategories() =>
            _db.LoadData<string, object>(
                "SELECT CategoryName FROM dbo.[MedicationCategory] WHERE IsActive = 1 ORDER BY CategoryName;",
                new { });
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
            return await _db.ExecuteScalarAsync("SELECT COUNT(*) FROM dbo.Medication;", new { });
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
                var insertSql = $"INSERT INTO dbo.[{tableName}] (CategoryName, CategoryDescription, IsActive) VALUES (@CategoryName, @Description, 1);";
                return _db.Save(insertSql, new { model.CategoryName, Description = model.Description ?? string.Empty });
            }

            var updateSql = $"UPDATE dbo.[{tableName}] SET CategoryName = @CategoryName, CategoryDescription = @Description WHERE CategoryName = @OriginalCategoryName;";
            return _db.Save(updateSql, new { model.CategoryName, Description = model.Description ?? string.Empty, model.OriginalCategoryName });
        }

        public Task<IEnumerable<CategoryItem>> GetCategoryDetails(string categoryType)
        {
            var tableName = GetCategoryTableName(categoryType);
            var sql = $"SELECT CategoryName, COALESCE(CategoryDescription, '') AS Description, IsActive FROM dbo.[{tableName}] ORDER BY CategoryName;";
            return _db.LoadData<CategoryItem, object>(sql, new { });
        }

        public async Task SetCategoryStatus(string categoryType, string categoryName, bool isActive)
        {
            var tableName = GetCategoryTableName(categoryType);
            var sql = $"UPDATE dbo.[{tableName}] SET IsActive = @IsActive WHERE CategoryName = @CategoryName;";
            var affected = await _db.Save(sql, new { CategoryName = categoryName, IsActive = isActive });
            if (affected == 0)
                throw new InvalidOperationException("Category not found.");
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
            return await _db.ExecuteScalarAsync("SELECT COUNT(*) FROM dbo.[User];", new { });
        }

        public Task<IEnumerable<ConsentDoctorOption>> GetActiveConsentDoctors() =>
            _db.LoadData<ConsentDoctorOption, object>(
                """
                SELECT D.DoctorID, U.Name, LEFT(U.Name, 1) AS FirstLetter,
                    U.Surname, U.Email, D.HPCSA_Number AS HPCSANumber
                FROM dbo.Doctor D
                INNER JOIN dbo.[User] U ON D.UserID = U.UserID
                WHERE U.IsActive = 1
                ORDER BY U.Surname, U.Name;
                """, new { });
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
            var credential = await _db.QueryFirstOrDefaultAsync<PasswordCredential>(
                "SELECT u.Email, u.PasswordHash, u.PasswordSalt FROM dbo.[User] u INNER JOIN dbo.Admin a ON a.UserID = u.UserID WHERE a.AdminID = @AdminID AND u.Role = 'Admin' AND u.IsActive = 1;",
                new { AdminID = adminId });
            await ChangeUserPassword(credential, model);
        }

        public async Task ChangePatientPassword(int patientId, UserPasswordChange model)
        {
            var credential = await _db.QueryFirstOrDefaultAsync<PasswordCredential>(
                "SELECT u.Email, u.PasswordHash, u.PasswordSalt FROM dbo.[User] u INNER JOIN dbo.Patient p ON p.UserID = u.UserID WHERE p.PatientID = @PatientID AND u.Role = 'Patient' AND u.IsActive = 1;",
                new { PatientID = patientId });
            await ChangeUserPassword(credential, model);
        }

        private async Task ChangeUserPassword(PasswordCredential? credential, UserPasswordChange model)
        {
            var errors = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
            if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(model,
                new System.ComponentModel.DataAnnotations.ValidationContext(model), errors, true))
                throw new InvalidOperationException(errors[0].ErrorMessage);
            if (credential == null)
                throw new InvalidOperationException("The active account could not be found.");
            if (!_pass.VerifyPassword(model.CurrentPassword, credential.PasswordHash, credential.PasswordSalt))
                throw new InvalidOperationException("The current password is incorrect.");

            var password = _pass.HashPassword(model.NewPassword);
            var result = await _db.SaveDataReturnId<PasswordChangeResult, object>(
                "dbo.sp_ChangeUserPassword", new
                {
                    credential.Email,
                    CurrentPasswordHash = credential.PasswordHash,
                    CurrentPasswordSalt = credential.PasswordSalt,
                    model.NewPassword,
                    model.ConfirmPassword,
                    NewPasswordHash = password.Hash,
                    NewPasswordSalt = password.Salt
                });
            if (result == null || result.Success != 1)
                throw new InvalidOperationException("The password could not be updated. Please try again.");
        }

        // Address lookup administration deliberately uses parameterised direct SQL.
        // These lookup tables do not have admin stored procedures and are shared by every user form.
        public async Task<AddressLookupManagementViewModel> GetAddressLookups()
        {
            var provinces = await _db.LoadData<AddressLookupProvince, object>(
                "SELECT ProvinceID, ProvinceName, IsActive FROM dbo.Province ORDER BY ProvinceName;", new { });
            var towns = await _db.LoadData<AddressLookupTownCity, object>(
                "SELECT t.TownCityID, t.ProvinceID, p.ProvinceName, t.TownCityName, t.IsActive FROM dbo.TownCity t INNER JOIN dbo.Province p ON p.ProvinceID = t.ProvinceID ORDER BY p.ProvinceName, t.TownCityName;", new { });
            var suburbs = await _db.LoadData<AddressLookupSuburb, object>(
                "SELECT s.SuburbID, s.TownCityID, p.ProvinceName, t.TownCityName, s.SuburbName, s.AreaCode, s.IsActive FROM dbo.Suburb s INNER JOIN dbo.TownCity t ON t.TownCityID = s.TownCityID INNER JOIN dbo.Province p ON p.ProvinceID = t.ProvinceID ORDER BY p.ProvinceName, t.TownCityName, s.SuburbName;", new { });

            return new AddressLookupManagementViewModel
            {
                Provinces = provinces.ToList(),
                TownCities = towns.ToList(),
                Suburbs = suburbs.ToList()
            };
        }

        public async Task SaveAddressLookup(AddressLookupSave model)
        {
            var type = model.LookupType.Trim().ToLowerInvariant();
            var name = model.Name.Trim();
            int affected;

            switch (type)
            {
                case "province":
                    affected = model.Id.HasValue
                        ? await _db.Save("UPDATE dbo.Province SET ProvinceName = @Name WHERE ProvinceID = @Id;", new { model.Id, Name = name })
                        : await _db.Save("INSERT dbo.Province (ProvinceName, IsActive) VALUES (@Name, 1);", new { Name = name });
                    break;
                case "towncity":
                    affected = model.Id.HasValue
                        ? await _db.Save("UPDATE dbo.TownCity SET ProvinceID = @ProvinceId, TownCityName = @Name WHERE TownCityID = @Id;", new { model.Id, model.ProvinceId, Name = name })
                        : await _db.Save("INSERT dbo.TownCity (ProvinceID, TownCityName, IsActive) VALUES (@ProvinceId, @Name, 1);", new { model.ProvinceId, Name = name });
                    break;
                case "suburb":
                    affected = model.Id.HasValue
                        ? await _db.Save("UPDATE dbo.Suburb SET TownCityID = @TownCityId, SuburbName = @Name, AreaCode = @AreaCode WHERE SuburbID = @Id;", new { model.Id, model.TownCityId, Name = name, AreaCode = model.AreaCode!.Trim() })
                        : await _db.Save("INSERT dbo.Suburb (TownCityID, SuburbName, AreaCode, IsActive) VALUES (@TownCityId, @Name, @AreaCode, 1);", new { model.TownCityId, Name = name, AreaCode = model.AreaCode!.Trim() });
                    break;
                default:
                    throw new ArgumentException("Invalid address lookup type.", nameof(model));
            }

            if (affected != 1)
                throw new InvalidOperationException("The selected address entry could not be found.");
        }

        public async Task SetAddressLookupStatus(string lookupType, int id, bool isActive)
        {
            var sql = lookupType.Trim().ToLowerInvariant() switch
            {
                "province" => "UPDATE dbo.Province SET IsActive = @IsActive WHERE ProvinceID = @Id;",
                "towncity" => "UPDATE dbo.TownCity SET IsActive = @IsActive WHERE TownCityID = @Id;",
                "suburb" => "UPDATE dbo.Suburb SET IsActive = @IsActive WHERE SuburbID = @Id;",
                _ => throw new ArgumentException("Invalid address lookup type.", nameof(lookupType))
            };

            if (await _db.Save(sql, new { Id = id, IsActive = isActive }) != 1)
                throw new InvalidOperationException("The selected address entry could not be found.");
        }
    }
    }
   

   


