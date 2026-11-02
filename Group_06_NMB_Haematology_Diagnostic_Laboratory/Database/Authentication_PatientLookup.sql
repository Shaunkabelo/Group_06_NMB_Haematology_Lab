CREATE OR ALTER PROCEDURE dbo.sp_Auth_GetPatientIdByUserId
    @UserID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.PatientID
    FROM dbo.Patient AS p
    INNER JOIN dbo.[User] AS u
        ON u.UserID = p.UserID
    WHERE p.UserID = @UserID
      AND u.IsActive = 1
      AND u.[Role] = 'Patient';
END;
GO