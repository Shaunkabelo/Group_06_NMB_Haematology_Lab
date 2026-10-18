-- READ ONLY. Run in the same database as ConnectionStrings:conn.
SET NOCOUNT ON;
DECLARE @Required TABLE (ObjectName SYSNAME, ColumnName SYSNAME);
INSERT @Required VALUES
 ('User','SecurityStamp'), ('User','MustChangePassword'),
 ('PatientConsent','TestRequestTestTypeID'), ('PatientConsentRequest','TestRequestID'),
 ('TestType','IsActive'), ('TestType','NormalRangeMin'), ('TestType','NormalRangeMax'),
 ('TestRequest','ReleasedByDoctorID'), ('TestRequest','DoctorReleaseNote'), ('TestRequest','ReleaseDate'),
 ('TestRequest','CancelledBy'), ('TestRequest','CancelledDate'), ('TestRequest','CancelReason'),
 ('TestResultVerification','VerifiedByLabTechnicianID'), ('TestResultVerification','VerificationDateTime'),
 ('DoctorEmailQueue','Payload'), ('DoctorEmailQueue','LeaseId');
SELECT ObjectName, ColumnName AS MissingColumn FROM @Required
WHERE COL_LENGTH(N'dbo.' + QUOTENAME(ObjectName), ColumnName) IS NULL;
IF EXISTS (SELECT 1 FROM @Required WHERE COL_LENGTH(N'dbo.' + QUOTENAME(ObjectName), ColumnName) IS NULL)
    THROW 51000, 'Doctor workflow schema is incomplete. Review the listed columns and deployment instructions.', 1;

SELECT p.name AS MissingAuthenticationProcedure FROM (VALUES
    ('sp_Auth_GetActiveUserByEmail'), ('sp_Auth_GetUserById'), ('sp_Auth_UpdateProfile'),
    ('sp_Auth_GetState'), ('sp_Auth_ChangePassword'), ('sp_Auth_CreateReset'), ('sp_Auth_ResetPassword'),
    ('spDoctor_GetByUserId'), ('spTestRequest_CancelByDoctor')) p(name)
WHERE OBJECT_ID(N'dbo.' + QUOTENAME(p.name), N'P') IS NULL;

-- Operational checks: metadata/counts only, no patient credentials or medical data.
SELECT Kind, COUNT(*) AS Pending, MAX(Attempts) AS MaximumAttempts
FROM dbo.DoctorEmailQueue WHERE SentAt IS NULL GROUP BY Kind;
SELECT name, is_disabled FROM sys.triggers WHERE name = 'tr_DoctorNotifyCancellation';
