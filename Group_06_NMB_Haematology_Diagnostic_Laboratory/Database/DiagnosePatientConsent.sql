-- Read-only. Run against the application's database.
SELECT DB_NAME() AS CurrentDatabase, @@SERVERNAME AS CurrentServer,
       compatibility_level AS CompatibilityLevel
FROM sys.databases WHERE name = DB_NAME();

SELECT required.ProcedureName,
       CASE WHEN OBJECT_ID(required.ProcedureName, 'P') IS NULL
            THEN 'Missing' ELSE 'Present' END AS ProcedureStatus
FROM (VALUES (N'dbo.sp_SavePatientConsent'),
             (N'dbo.sp_SavePatientConsents')) required(ProcedureName);

SELECT c.name AS ColumnName, TYPE_NAME(c.user_type_id) AS DataType,
       c.is_nullable AS IsNullable
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(N'dbo.PatientConsent');

-- SavePatientConsent.sql requires PatientConsent.TestRequestTestTypeID.
-- OPENJSON requires database compatibility level 130 or higher.
-- If either procedure is missing, run the ENTIRE SavePatientConsent.sql file.
SELECT OBJECT_DEFINITION(OBJECT_ID(N'dbo.sp_SavePatientConsent')) AS SingleSaveDefinition,
       OBJECT_DEFINITION(OBJECT_ID(N'dbo.sp_SavePatientConsents')) AS BatchSaveDefinition;
