-- Run against the application's database. Repairs credentials for patient 9975 only.
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    UPDATE u
    SET PasswordHash = N'zs6KieFCi4wYNMW74DswC+wuMIuI9ekO/WThxDGn+uMiYMzhfEJmPJiEL3z1QF0nEFqRydj1IZeWUjiHKyaEEg==',
        PasswordSalt = N'jfrUNLiob+zIyan/FJ2yd8bekcpiNPsMXa1U00MTvCEtigocKP4XDNBzN/ls11Zxq/FmasAOOPpZw39+e8/+/CXCloETlL6JtbLja1IVcQOf+8KJjREjsKPvK0jJAYw5DyMcPszCNgeRMuObSHohZQ7SWXgXyAkrFUsiiu0szEw=',
        UpdatedAt = GETUTCDATE()
    FROM dbo.[User] u
    INNER JOIN dbo.Patient p ON p.UserID = u.UserID
    WHERE p.PatientID = 9975
      AND u.Role = N'Patient'
      AND u.IsActive = 1;

    IF @@ROWCOUNT <> 1
        THROW 50001, 'Expected one active patient with PatientID 9975. No changes were saved.', 1;

    COMMIT TRANSACTION;
    PRINT 'Patient 9975 password repaired.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

