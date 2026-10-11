CREATE OR ALTER PROCEDURE dbo.sp_SavePatientConsent
    @PatientID INT,
    @DoctorID INT,
    @TestRequestTestTypeID INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE PatientConsent
    SET IsActive = 1, GrantedDate = GETDATE()
    WHERE PatientID = @PatientID AND DoctorID = @DoctorID
        AND TestRequestTestTypeID = @TestRequestTestTypeID;
    IF @@ROWCOUNT = 0
        INSERT INTO PatientConsent (PatientID, DoctorID, TestRequestTestTypeID, GrantedDate, IsActive)
        VALUES (@PatientID, @DoctorID, @TestRequestTestTypeID, GETDATE(), 1);
END;
GO

-- Validate and save all selected tests together, using the single-test procedure.
CREATE OR ALTER PROCEDURE dbo.sp_SavePatientConsents
    @PatientID INT,
    @DoctorID INT,
    @TestIDs NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Selected TABLE (ID INT PRIMARY KEY);
    INSERT INTO @Selected SELECT DISTINCT CAST(value AS INT) FROM OPENJSON(@TestIDs);
    BEGIN TRY
        BEGIN TRANSACTION;
        IF NOT EXISTS (SELECT 1 FROM @Selected)
            THROW 50001, 'Select at least one test.', 1;
        IF NOT EXISTS (SELECT 1 FROM Doctor d INNER JOIN [User] u ON u.UserID = d.UserID
                       WHERE d.DoctorID = @DoctorID AND u.IsActive = 1)
            THROW 50002, 'Doctor is unavailable.', 1;
        IF EXISTS (SELECT 1 FROM @Selected s WHERE NOT EXISTS (
            SELECT 1 FROM TestRequestTestType t INNER JOIN TestRequest r ON r.TestRequestID = t.TestRequestID
            WHERE t.TestRequestTestTypeID = s.ID AND r.PatientID = @PatientID
              AND LOWER(LTRIM(RTRIM(r.Status))) IN ('released by doctor', 'released by the doctor')))
            THROW 50003, 'A selected test is unavailable for sharing.', 1;
        DECLARE @TestID INT;
        WHILE EXISTS (SELECT 1 FROM @Selected)
        BEGIN
            SELECT TOP (1) @TestID = ID FROM @Selected ORDER BY ID;
            IF NOT EXISTS (SELECT 1 FROM PatientConsent WITH (UPDLOCK, HOLDLOCK)
                WHERE PatientID = @PatientID AND DoctorID = @DoctorID
                  AND TestRequestTestTypeID = @TestID AND IsActive = 1)
                EXEC dbo.sp_SavePatientConsent @PatientID, @DoctorID, @TestID;
            DELETE FROM @Selected WHERE ID = @TestID;
        END;
        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;
END;
GO
