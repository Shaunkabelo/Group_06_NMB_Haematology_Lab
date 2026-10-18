-- Read-only preflight. Run against the exact database configured in ConnectionStrings:conn.
SET NOCOUNT ON;

DECLARE @RequiredProcedures TABLE (ProcedureName SYSNAME NOT NULL PRIMARY KEY);
INSERT INTO @RequiredProcedures (ProcedureName) VALUES
    (N'sp_Auth_GetActiveUserByEmail'),
    (N'sp_Auth_RecordLogin'),
    (N'sp_Auth_GetState'),
    (N'sp_Auth_CreateReset'),
    (N'sp_Auth_ResetPassword'),
    (N'sp_Auth_ChangePassword'),
    (N'spUser_GetByEmail'),
    (N'spUser_GetById'),
    (N'spUser_UpdatePassword'),
    (N'spDoctor_GetByUserId'),
    (N'spDoctorAccount_GetByUserId'),
    (N'spDoctorAccount_Update'),
    (N'spDoctorReport_GetRequests'),
    (N'spDoctorResults_GetAll'),
    (N'spDoctorAlerts_GetAbnormal'),
    (N'spDoctorResult_GetByLabTestId'),
    (N'spPatient_GetAll'),
    (N'spDoctor_Patient_GetById'),
    (N'spPatient_GetBySaId'),
    (N'spPatient_Register'),
    (N'spTestCategory_GetAll'),
    (N'spTestType_GetByCategory'),
    (N'spSample_Insert'),
    (N'spTestRequestTestType_Insert'),
    (N'spTestRequest_Create'),
    (N'spTestRequest_CancelByDoctor'),
    (N'spMedicalCondition_GetAll'),
    (N'spMedicalCondition_GetCategories'),
    (N'spMedicalCondition_GetByCategory'),
    (N'spPatientCondition_Add'),
    (N'spPatientCondition_Delete'),
    (N'spAllergy_GetAll'),
    (N'spAllergy_GetCategories'),
    (N'spAllergy_GetByCategory'),
    (N'spPatientAllergy_Add'),
    (N'spPatientAllergy_Delete'),
    (N'spMedication_GetAll'),
    (N'spMedication_GetCategories'),
    (N'spMedication_GetByCategory'),
    (N'spPatientMedication_Add'),
    (N'spPatientMedication_Delete');

DECLARE @MissingProcedures TABLE (ProcedureName SYSNAME NOT NULL PRIMARY KEY);
INSERT INTO @MissingProcedures (ProcedureName)
SELECT required.ProcedureName
FROM @RequiredProcedures AS required
WHERE OBJECT_ID(N'dbo.' + QUOTENAME(required.ProcedureName), N'P') IS NULL;

SELECT DB_NAME() AS CheckedDatabase;
SELECT ProcedureName AS MissingProcedure FROM @MissingProcedures ORDER BY ProcedureName;

IF TYPE_ID(N'dbo.TestRequestTestTypeTableType') IS NULL
    THROW 51001, 'Required table type dbo.TestRequestTestTypeTableType is missing.', 1;

IF EXISTS (SELECT 1 FROM @MissingProcedures)
    THROW 51002, 'One or more required subsystem stored procedures are missing.', 1;

PRINT 'Database preflight passed: all listed stored procedures and the test-request table type exist.';
