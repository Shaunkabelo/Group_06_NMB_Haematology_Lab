CREATE OR ALTER PROCEDURE dbo.sp_Patient_GetTestRequestProgress
    @PatientID INT,
    @TestRequestID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT tt.TestName, trtt.Status AS TestStatus,
        lt.LabTechnicianID, lt.StartDateTime,
        NULLIF(LTRIM(RTRIM(CONCAT(u.Name, ' ', u.Surname))), '') AS LabTechnicianName
    FROM dbo.TestRequest tr
    INNER JOIN dbo.TestRequestTestType trtt ON tr.TestRequestID = trtt.TestRequestID
    INNER JOIN dbo.TestType tt ON trtt.TestTypeID = tt.TestTypeID
    LEFT JOIN dbo.LabTest lt ON trtt.TestRequestTestTypeID = lt.TestRequestTestTypeID
    LEFT JOIN dbo.LabTechnician tech ON tech.LabTechnicianID = lt.LabTechnicianID
    LEFT JOIN dbo.[User] u ON u.UserID = tech.UserID
    WHERE tr.TestRequestID = @TestRequestID AND tr.PatientID = @PatientID
    ORDER BY tt.TestName ASC;
END;
GO
