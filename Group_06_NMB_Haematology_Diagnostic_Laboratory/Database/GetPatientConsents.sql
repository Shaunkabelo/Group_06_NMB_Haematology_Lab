CREATE OR ALTER PROCEDURE dbo.sp_GetPatientConsents
    @PatientID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT d.DoctorID, 'Dr. ' + LEFT(u.Name, 1) + '. ' + u.Surname AS DoctorName,
        u.Email, d.HPCSANumber,
        CAST(NULL AS NVARCHAR(100)) AS Specialisation,
        tr.TestRequestID,
        'REQ-' + CAST(YEAR(tr.RequestDate) AS VARCHAR(4)) + '-' +
            RIGHT('00000' + CAST(tr.TestRequestID AS VARCHAR(10)), 5) AS RequestNumber,
        pc.TestRequestTestTypeID,
        CAST(MAX(CAST(pc.IsActive AS INT)) AS BIT) AS IsActive
    FROM PatientConsent pc
    INNER JOIN Doctor d ON d.DoctorID = pc.DoctorID
    INNER JOIN [User] u ON u.UserID = d.UserID
    INNER JOIN TestRequestTestType t ON t.TestRequestTestTypeID = pc.TestRequestTestTypeID
    INNER JOIN TestRequest tr ON tr.TestRequestID = t.TestRequestID
    WHERE pc.PatientID = @PatientID AND tr.PatientID = @PatientID
    GROUP BY d.DoctorID, u.Name, u.Surname, u.Email, d.HPCSANumber,
        tr.TestRequestID, tr.RequestDate, pc.TestRequestTestTypeID
    ORDER BY u.Surname, u.Name, tr.TestRequestID DESC;
END;
GO
