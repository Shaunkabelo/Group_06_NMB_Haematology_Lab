CREATE OR ALTER PROCEDURE [dbo].[sp_GetPatientRequests]
    @PatientID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        tr.TestRequestID,
        'REQ-' + CAST(YEAR(tr.RequestDate) AS VARCHAR(4)) + '-' +
            RIGHT('00000' + CAST(tr.TestRequestID AS VARCHAR(10)), 5) AS RequestNumber,
        STRING_AGG(tt.TestName, ', ') AS TestsRequested,
        'Dr. ' + LEFT(u.Name, 1) + '. ' + u.Surname AS DoctorName,
        tr.RequestDate,
        tr.Urgency,
        tr.Status,
        tr.ClinicalNotes,
        tr.DoctorReleaseNote,
        tr.ReleaseDate
    FROM dbo.TestRequest tr
    INNER JOIN dbo.Patient p ON tr.PatientID = p.PatientID
    INNER JOIN dbo.Doctor d ON tr.DoctorID = d.DoctorID
    INNER JOIN dbo.[User] u ON d.UserID = u.UserID
    INNER JOIN dbo.TestRequestTestType trtt ON tr.TestRequestID = trtt.TestRequestID
    INNER JOIN dbo.TestType tt ON trtt.TestTypeID = tt.TestTypeID
    WHERE tr.PatientID = @PatientID
    GROUP BY tr.TestRequestID, tr.RequestDate, tr.Urgency, tr.Status,
        tr.ClinicalNotes, tr.DoctorReleaseNote, tr.ReleaseDate, u.Name, u.Surname
    ORDER BY tr.RequestDate DESC;
END;
GO
