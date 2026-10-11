CREATE OR ALTER PROCEDURE [dbo].[sp_GetAllDoctors]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        D.DoctorID,
        LEFT(U.[Name], 1) AS FirstLetter,
        U.[Surname],
        U.[Email]
    FROM [dbo].[Doctor] D
    INNER JOIN [dbo].[User] U ON D.[UserID] = U.[UserID]
    WHERE U.[IsActive] = 1
    ORDER BY U.[Surname] ASC, U.[Name] ASC;
END
GO
