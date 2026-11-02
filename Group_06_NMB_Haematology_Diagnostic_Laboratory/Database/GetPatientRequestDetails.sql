CREATE OR ALTER PROCEDURE dbo.sp_Patient_GetTestRequestDetails
    @PatientID INT,
    @TestRequestID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT trtt.TestRequestTestTypeID, tt.TestName, trr.ResultValue, trr.Notes AS ResultNotes, tt.UnitOfMeasurement,
        CASE
            WHEN tt.NormalRangeMin IS NOT NULL AND tt.NormalRangeMax IS NOT NULL
                THEN CAST(tt.NormalRangeMin AS NVARCHAR(50)) + ' - ' + CAST(tt.NormalRangeMax AS NVARCHAR(50))
            WHEN tt.NormalRangeMin IS NOT NULL
                THEN '>= ' + CAST(tt.NormalRangeMin AS NVARCHAR(50))
            WHEN tt.NormalRangeMax IS NOT NULL
                THEN '<= ' + CAST(tt.NormalRangeMax AS NVARCHAR(50))
            ELSE 'N/A'
        END AS ReferenceRange,
        CASE
            WHEN trr.ResultValue IS NULL THEN 'Pending'
            WHEN tt.NormalRangeMin IS NOT NULL AND trr.ResultValue < tt.NormalRangeMin THEN 'Low'
            WHEN tt.NormalRangeMax IS NOT NULL AND trr.ResultValue > tt.NormalRangeMax THEN 'High'
            ELSE 'Normal'
        END AS ResultFlag
    FROM TestRequest tr
    INNER JOIN TestRequestTestType trtt ON tr.TestRequestID = trtt.TestRequestID
    INNER JOIN TestType tt ON trtt.TestTypeID = tt.TestTypeID
    LEFT JOIN LabTest lt ON trtt.TestRequestTestTypeID = lt.TestRequestTestTypeID
    LEFT JOIN TestResult trr ON lt.LabTestID = trr.LabTestID
    WHERE tr.TestRequestID = @TestRequestID AND tr.PatientID = @PatientID
        AND LOWER(LTRIM(RTRIM(tr.Status))) IN ('released by doctor', 'released by the doctor')
    ORDER BY tt.TestName ASC;
END;
GO
