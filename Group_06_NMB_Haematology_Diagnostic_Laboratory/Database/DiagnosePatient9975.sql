-- Read-only: run in the same database as the password repair.
SELECT DB_NAME() AS CurrentDatabase, @@SERVERNAME AS CurrentServer;

SELECT p.PatientID, p.UserID AS PatientUserID, u.UserID,
       u.Email, u.[Role], u.IsActive,
       CASE
           WHEN p.PatientID IS NULL THEN 'User exists but has no patient record'
           WHEN u.UserID IS NULL THEN 'Patient exists but linked user is missing'
           WHEN p.PatientID <> 9975 THEN '9975 is the UserID; use the displayed PatientID'
           WHEN u.[Role] IS NULL OR u.[Role] <> N'Patient' THEN 'Linked user does not have the Patient role'
           WHEN u.IsActive IS NULL OR u.IsActive <> 1 THEN 'Patient account is inactive'
           ELSE 'Matches the password repair filters'
       END AS Diagnosis
FROM dbo.Patient p
FULL OUTER JOIN dbo.[User] u ON u.UserID = p.UserID
WHERE p.PatientID = 9975 OR p.UserID = 9975 OR u.UserID = 9975;

-- No rows in the second result means neither ID exists in this database.
