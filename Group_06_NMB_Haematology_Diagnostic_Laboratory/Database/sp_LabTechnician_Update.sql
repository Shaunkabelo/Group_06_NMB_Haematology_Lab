USE [GRP_04_06_db]
GO

ALTER PROCEDURE [dbo].[sp_LabTechnician_Update]
(
    @LabTechnicianID INT,
    @Name NVARCHAR(100),
    @Surname NVARCHAR(100),
    @Email NVARCHAR(150),
    @Cellphone NVARCHAR(20),
    @HomeAddress NVARCHAR(255),
    @SA_ID_Number NVARCHAR(13),
    @DateOfBirth DATE,
    @EmployeeNumber NVARCHAR(50)
)
AS
BEGIN
    SET NOCOUNT ON;

    BEGIN TRANSACTION;

    BEGIN TRY
        UPDATE u
        SET
            Name = @Name,
            Surname = @Surname,
            Email = @Email,
            Cellphone = @Cellphone,
            HomeAddress = @HomeAddress,
            SA_ID_Number = @SA_ID_Number,
            DateOfBirth = @DateOfBirth,
            UpdatedAt = GETUTCDATE()
        FROM [User] u
        INNER JOIN LabTechnician lt ON u.UserID = lt.UserID
        WHERE lt.LabTechnicianID = @LabTechnicianID;

        UPDATE LabTechnician
        SET EmployeeNumber = @EmployeeNumber
        WHERE LabTechnicianID = @LabTechnicianID;

        COMMIT;
    END TRY
    BEGIN CATCH
        ROLLBACK;
        THROW;
    END CATCH
END
GO
