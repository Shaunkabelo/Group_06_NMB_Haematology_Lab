CREATE OR ALTER PROCEDURE dbo.sp_Patient_GetTestRequestProgress
    @PatientID INT,
    @TestRequestID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT tt.TestName, trtt.Status AS TestStatus,
        lt.LabTechnicianID, lt.StartDateTime
    FROM TestRequest tr
    INNER JOIN TestRequestTestType trtt ON tr.TestRequestID = trtt.TestRequestID
    INNER JOIN TestType tt ON trtt.TestTypeID = tt.TestTypeID
    LEFT JOIN LabTest lt ON trtt.TestRequestTestTypeID = lt.TestRequestTestTypeID
    WHERE tr.TestRequestID = @TestRequestID AND tr.PatientID = @PatientID
    ORDER BY tt.TestName ASC;
END;
GO
