CREATE OR ALTER PROCEDURE dbo.sp_Patient_GetReleasedTestRequests
    @PatientID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT tr.TestRequestID AS RequestID,
        'REQ-' + CAST(YEAR(tr.RequestDate) AS VARCHAR(4)) + '-' +
        RIGHT('00000' + CAST(tr.TestRequestID AS VARCHAR(10)), 5) AS TestRequestID,
        'Dr. ' + u.Name + ' ' + u.Surname AS DoctorName,
        tr.RequestDate
    FROM TestRequest tr
    LEFT JOIN Doctor d ON d.DoctorID = tr.DoctorID
    LEFT JOIN [User] u ON u.UserID = d.UserID
    WHERE tr.PatientID = @PatientID AND tr.Status = 'Released by doctor'
    ORDER BY tr.RequestDate DESC, tr.TestRequestID DESC;
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_Patient_GetConsentRequestTests
    @PatientID INT,
    @TestRequestID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT trtt.TestRequestTestTypeID, tt.TestName
    FROM TestRequest tr
    INNER JOIN TestRequestTestType trtt ON tr.TestRequestID = trtt.TestRequestID
    INNER JOIN TestType tt ON trtt.TestTypeID = tt.TestTypeID
    WHERE tr.PatientID = @PatientID AND tr.TestRequestID = @TestRequestID
        AND tr.Status = 'Released by doctor'
    ORDER BY tt.TestName ASC;
END;
GO
