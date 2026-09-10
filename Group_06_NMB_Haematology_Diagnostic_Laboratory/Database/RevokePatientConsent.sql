CREATE OR ALTER PROCEDURE dbo.sp_RevokePatientConsent
    @PatientID INT,
    @DoctorID INT,
    @TestRequestTestTypeID INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.PatientConsent
    SET IsActive = 0
    WHERE PatientID = @PatientID AND DoctorID = @DoctorID
        AND TestRequestTestTypeID = @TestRequestTestTypeID AND IsActive = 1;
END;
GO

-- Revoke the selected doctor's displayed tests as one operation.
CREATE OR ALTER PROCEDURE dbo.sp_RevokePatientConsents
    @PatientID INT,
    @DoctorID INT,
    @TestIDs NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    DECLARE @Selected TABLE (ID INT PRIMARY KEY);
    INSERT INTO @Selected SELECT DISTINCT CAST(value AS INT) FROM OPENJSON(@TestIDs);
    IF NOT EXISTS (SELECT 1 FROM @Selected) OR EXISTS (SELECT 1 FROM @Selected WHERE ID <= 0)
        THROW 50001, 'Select at least one valid test.', 1;
    BEGIN TRY
        BEGIN TRANSACTION;
        DECLARE @TestID INT;
        WHILE EXISTS (SELECT 1 FROM @Selected)
        BEGIN
            SELECT TOP (1) @TestID = ID FROM @Selected ORDER BY ID;
            EXEC dbo.sp_RevokePatientConsent @PatientID, @DoctorID, @TestID;
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
