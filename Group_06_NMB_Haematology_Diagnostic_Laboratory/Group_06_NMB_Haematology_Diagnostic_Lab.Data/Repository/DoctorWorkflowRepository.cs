using System.Data;
using Dapper;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;

public sealed class DoctorWorkflowRepository(IConfiguration configuration) : IDoctorWorkflowRepository
{
    private SqlConnection Connection() => new(configuration.GetConnectionString("conn"));

    // Support current per-test grants and legacy request grants; revocation is rechecked on every read.
    private const string Access = """
        (r.DoctorID = @DoctorID OR (r.Status = N'Released by doctor' AND EXISTS (
            SELECT 1 FROM dbo.PatientConsent pc
            WHERE pc.PatientID = r.PatientID AND pc.DoctorID = @DoctorID
              AND pc.IsActive = 1 AND (
                EXISTS (SELECT 1 FROM dbo.TestRequestTestType shared WHERE shared.TestRequestID = r.TestRequestID
                    AND shared.TestRequestTestTypeID = pc.TestRequestTestTypeID)
                OR (pc.TestRequestTestTypeID IS NULL AND EXISTS (SELECT 1 FROM dbo.PatientConsentRequest pcr
                    WHERE pcr.PatientConsentID = pc.PatientConsentID AND pcr.TestRequestID = r.TestRequestID))))))
        """;
    private const string TestAccess = """
        (r.DoctorID = @DoctorID OR (r.Status = N'Released by doctor' AND EXISTS (
            SELECT 1 FROM dbo.PatientConsent pc WHERE pc.PatientID = r.PatientID AND pc.DoctorID = @DoctorID
            AND pc.IsActive = 1 AND (pc.TestRequestTestTypeID = rt.TestRequestTestTypeID
                OR (pc.TestRequestTestTypeID IS NULL AND EXISTS (SELECT 1 FROM dbo.PatientConsentRequest pcr
                    WHERE pcr.PatientConsentID = pc.PatientConsentID AND pcr.TestRequestID = r.TestRequestID))))))
        """;
    private const string RequestSelect = """
        SELECT r.TestRequestID AS Id, r.PatientID, r.DoctorID,
            CONCAT(u.Name, ' ', u.Surname) AS PatientName, u.SA_ID_Number AS SaIdNumber, u.Email,
            r.RequestDate AS CreatedAt, r.Status, r.Urgency,
            CASE WHEN r.DoctorID = @DoctorID THEN r.ClinicalNotes ELSE NULL END AS ClinicalNotes,
            r.CancelReason AS CancellationReason, r.CancelledBy, r.CancelledDate,
            r.ReleaseDate, CASE WHEN r.DoctorID = @DoctorID THEN r.DoctorReleaseNote ELSE NULL END AS DoctorReleaseNote,
            CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.PatientConsent pc
                WHERE pc.PatientID = r.PatientID AND pc.DoctorID = @DoctorID AND pc.IsActive = 1
                  AND (EXISTS (SELECT 1 FROM dbo.TestRequestTestType shared WHERE shared.TestRequestID = r.TestRequestID
                        AND shared.TestRequestTestTypeID = pc.TestRequestTestTypeID)
                    OR (pc.TestRequestTestTypeID IS NULL AND EXISTS (SELECT 1 FROM dbo.PatientConsentRequest pcr
                        WHERE pcr.PatientConsentID = pc.PatientConsentID AND pcr.TestRequestID = r.TestRequestID)))) THEN 1 ELSE 0 END AS BIT) AS ConsentGranted,
            (SELECT STRING_AGG(CAST(tt.TestName AS NVARCHAR(MAX)), ', ')
                FROM dbo.TestRequestTestType rt INNER JOIN dbo.TestType tt ON tt.TestTypeID = rt.TestTypeID
                WHERE rt.TestRequestID = r.TestRequestID AND
        """ + TestAccess + """
                ) AS TestNames
        FROM dbo.TestRequest r INNER JOIN dbo.Patient p ON p.PatientID = r.PatientID
        INNER JOIN dbo.[User] u ON u.UserID = p.UserID
        """;

    public async Task<IEnumerable<DoctorRequestRecord>> Requests(int doctorId, int? patientId = null)
    {
        using var db = Connection();
        return await db.QueryAsync<DoctorRequestRecord>(RequestSelect + " WHERE " + Access +
            " AND (@PatientID IS NULL AND r.DoctorID = @DoctorID OR r.PatientID = @PatientID) ORDER BY r.RequestDate DESC",
            new { DoctorID = doctorId, PatientID = patientId });
    }

    public async Task<DoctorRequestRecord?> Request(int doctorId, int requestId)
    {
        using var db = Connection();
        return await db.QuerySingleOrDefaultAsync<DoctorRequestRecord>(RequestSelect + " WHERE " + Access +
            " AND r.TestRequestID = @RequestID", new { DoctorID = doctorId, RequestID = requestId });
    }

    public async Task<IEnumerable<DoctorRequestItem>> Items(int doctorId, int requestId)
    {
        using var db = Connection();
        return await db.QueryAsync<DoctorRequestItem>("""
            SELECT tt.TestName AS TestType, rt.Status,
                (SELECT STRING_AGG(CAST(s.BarcodeNumber AS NVARCHAR(MAX)), ', ') FROM dbo.Sample s
                 WHERE s.TestRequestID = r.TestRequestID AND s.SampleTypeID = tt.SampleTypeID) AS SampleBarcode
            FROM dbo.TestRequest r INNER JOIN dbo.TestRequestTestType rt ON rt.TestRequestID = r.TestRequestID
            INNER JOIN dbo.TestType tt ON tt.TestTypeID = rt.TestTypeID
            WHERE r.TestRequestID = @RequestID AND
            """ + TestAccess, new { DoctorID = doctorId, RequestID = requestId });
    }

    private const string ResultSelect = """
        SELECT r.TestRequestID, r.RequestDate, r.Status AS RequestStatus, r.Urgency, r.ReleaseDate,
            CONCAT(u.Name, ' ', u.Surname) AS PatientName, u.SA_ID_Number AS PatientSaId,
            tt.TestName, lt.LabTestID, res.TestResultID, res.ResultValue, res.ResultText, res.Notes,
            CAST(CASE WHEN res.IsAbnormal = 1 OR res.ResultValue < tt.NormalRangeMin
                OR res.ResultValue > tt.NormalRangeMax THEN 1 ELSE 0 END AS BIT) AS IsAbnormal,
            res.CaptureDateTime,
            CASE WHEN v.Status = 'Verified' AND (rt.Status <> 'Verified' OR rt.Status IS NULL
                OR v.VerificationDateTime IS NULL OR res.CaptureDateTime IS NULL
                OR v.VerificationDateTime < res.CaptureDateTime OR v.VerifiedByLabTechnicianID = res.CapturedByLabTechnicianID)
                THEN 'Pending re-verification' ELSE v.Status END AS VerificationStatus, v.VerificationDateTime,
            tt.UnitOfMeasurement, tt.NormalRangeMin, tt.NormalRangeMax
        FROM dbo.TestRequest r INNER JOIN dbo.Patient p ON p.PatientID = r.PatientID
        INNER JOIN dbo.[User] u ON u.UserID = p.UserID
        INNER JOIN dbo.TestRequestTestType rt ON rt.TestRequestID = r.TestRequestID
        INNER JOIN dbo.TestType tt ON tt.TestTypeID = rt.TestTypeID
        INNER JOIN dbo.LabTest lt ON lt.TestRequestTestTypeID = rt.TestRequestTestTypeID
        INNER JOIN dbo.TestResult res ON res.LabTestID = lt.LabTestID
        OUTER APPLY (SELECT TOP (1) tv.Status, tv.VerificationDateTime, tv.VerifiedByLabTechnicianID
            FROM dbo.TestResultVerification tv WHERE tv.TestResultID = res.TestResultID
            ORDER BY tv.VerificationDateTime DESC, tv.TestResultVerificationID DESC) v
        """;

    public async Task<IEnumerable<DoctorResultRowDto>> Results(int doctorId, int? requestId = null)
    {
        using var db = Connection();
        return await db.QueryAsync<DoctorResultRowDto>(ResultSelect + " WHERE " + TestAccess +
            " AND (@RequestID IS NULL AND r.DoctorID = @DoctorID OR r.TestRequestID = @RequestID)" +
            " AND (r.DoctorID = @DoctorID OR (rt.Status = 'Verified' AND v.Status = 'Verified' AND v.VerificationDateTime >= res.CaptureDateTime AND v.VerifiedByLabTechnicianID <> res.CapturedByLabTechnicianID))",
            new { DoctorID = doctorId, RequestID = requestId });
    }

    public async Task<IEnumerable<DoctorAlertDto>> Alerts(int doctorId, DateTime start, DateTime endExclusive)
    {
        using var db = Connection();
        return await db.QueryAsync<DoctorAlertDto>(ResultSelect + """
             WHERE r.DoctorID = @DoctorID AND res.CaptureDateTime >= @Start AND res.CaptureDateTime < @End
             AND (res.IsAbnormal = 1 OR res.ResultValue < tt.NormalRangeMin OR res.ResultValue > tt.NormalRangeMax)
             ORDER BY res.CaptureDateTime DESC
            """, new { DoctorID = doctorId, Start = start, End = endExclusive });
    }

    public async Task<PatientDetailsDto?> Patient(int patientId)
    {
        using var db = Connection();
        return await db.QuerySingleOrDefaultAsync<PatientDetailsDto>("""
            SELECT p.PatientID, u.UserID, u.Name, u.Surname, u.Email, u.Cellphone,
                u.SA_ID_Number AS SaIdNumber, u.DateOfBirth, u.HomeAddress AS Address
            FROM dbo.Patient p INNER JOIN dbo.[User] u ON u.UserID = p.UserID
            WHERE p.PatientID = @patientId AND u.IsActive = 1 AND u.Role = 'Patient'
            """, new { patientId });
    }

    private static Task Queue(SqlConnection db, IDbTransaction transaction, string payload, string kind, int? requestId = null) =>
        db.ExecuteAsync("INSERT dbo.DoctorEmailQueue(TestRequestID, Kind, Payload) VALUES (@requestId, @kind, @payload)",
            new { requestId, kind, payload }, transaction);

    public async Task<string> Register(PatientRegistrationViewModel model, string hash, string salt, string notification)
    {
        using var db = Connection();
        await db.OpenAsync();
        using var tx = db.BeginTransaction(IsolationLevel.Serializable);
        if (await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.[User] WITH (UPDLOCK, HOLDLOCK) WHERE Email = @Email", model, tx) > 0)
            return "DUPLICATE_EMAIL";
        if (await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.[User] WITH (UPDLOCK, HOLDLOCK) WHERE SA_ID_Number = @SaIdNumber", model, tx) > 0)
            return "DUPLICATE_ID";
        var userId = await db.QuerySingleAsync<int>("""
            INSERT dbo.[User](Name, Surname, Email, Cellphone, HomeAddress, SA_ID_Number, DateOfBirth,
                PasswordHash, PasswordSalt, Role, IsActive, MustChangePassword, CreatedAt, SecurityStamp)
            OUTPUT INSERTED.UserID
            VALUES (@Name, @Surname, @Email, @Cellphone, @HomeAddress, @SaIdNumber, @DateOfBirth,
                @hash, @salt, 'Patient', 1, 1, SYSUTCDATETIME(), NEWID())
            """, new { model.Name, model.Surname, model.Email, model.Cellphone, model.HomeAddress,
                model.SaIdNumber, model.DateOfBirth, hash, salt }, tx);
        await db.ExecuteAsync("INSERT dbo.Patient(UserID) VALUES (@userId)", new { userId }, tx);
        await Queue(db, tx, notification, "Registration");
        tx.Commit();
        return "SUCCESS";
    }

    public async Task<int> Create(int doctorId, CreateTestRequestModel model, string notification)
    {
        using var db = Connection();
        await db.OpenAsync();
        using var tx = db.BeginTransaction(IsolationLevel.Serializable);
        if (await db.ExecuteScalarAsync<int>("""
            SELECT COUNT(*) FROM dbo.Patient p INNER JOIN dbo.[User] u ON u.UserID = p.UserID
            WHERE p.PatientID = @PatientId AND u.IsActive = 1 AND u.Role = 'Patient'
            """, model, tx) != 1) throw new ArgumentException("Select an active patient.");
        foreach (var test in model.Tests)
        {
            if (await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.TestType WHERE TestTypeID = @TestTypeId AND SampleTypeID = @SampleTypeId AND IsActive = 1", test, tx) != 1)
                throw new ArgumentException("A selected test does not match its required sample type.");
            if (await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.Sample WITH (UPDLOCK, HOLDLOCK) WHERE BarcodeNumber = @BarcodeNumber", test, tx) > 0)
                throw new ArgumentException("A sample barcode has already been used on another request.");
        }
        var id = await db.QuerySingleAsync<int>("""
            INSERT dbo.TestRequest(PatientID, DoctorID, RequestDate, Urgency, ClinicalNotes, Status)
            OUTPUT INSERTED.TestRequestID
            VALUES (@PatientId, @doctorId, SYSDATETIME(), @Urgency, @ClinicalNotes, 'Submitted')
            """, new { model.PatientId, doctorId, model.Urgency, model.ClinicalNotes }, tx);
        foreach (var test in model.Tests)
            await db.ExecuteAsync("""
                INSERT dbo.TestRequestTestType(TestRequestID, TestTypeID, Status) VALUES (@id, @TestTypeId, 'Submitted')
                """, new { id, test.TestTypeId }, tx);
        foreach (var sample in model.Tests.DistinctBy(t => t.BarcodeNumber))
            await db.ExecuteAsync("""
                INSERT dbo.Sample(TestRequestID, SampleTypeID, BarcodeNumber) VALUES (@id, @SampleTypeId, @BarcodeNumber)
                """, new { id, sample.SampleTypeId, sample.BarcodeNumber }, tx);
        await Queue(db, tx, notification, "Request", id);
        tx.Commit();
        return id;
    }

    // Lock request and result/verification rows until release and notification have committed together.
    private const string Ready = """
        SELECT COUNT(*) FROM dbo.TestRequest r WITH (UPDLOCK, HOLDLOCK)
        WHERE r.TestRequestID = @requestId AND r.DoctorID = @doctorId
          AND r.Status IN ('Completed', 'Released by doctor')
          AND EXISTS (SELECT 1 FROM dbo.TestRequestTestType rt WHERE rt.TestRequestID = r.TestRequestID)
          AND NOT EXISTS (
            SELECT 1 FROM dbo.TestRequestTestType rt
                LEFT JOIN dbo.LabTest lt ON lt.TestRequestTestTypeID = rt.TestRequestTestTypeID
                LEFT JOIN dbo.TestResult res ON res.LabTestID = lt.LabTestID
                OUTER APPLY (SELECT TOP (1) tv.Status, tv.VerificationDateTime, tv.VerifiedByLabTechnicianID
                    FROM dbo.TestResultVerification tv WHERE tv.TestResultID = res.TestResultID
                    ORDER BY tv.VerificationDateTime DESC, tv.TestResultVerificationID DESC) v
            WHERE rt.TestRequestID = r.TestRequestID AND
                (ISNULL(rt.Status, '') <> 'Verified' OR res.TestResultID IS NULL OR ISNULL(v.Status, '') <> 'Verified'
                 OR v.VerifiedByLabTechnicianID = res.CapturedByLabTechnicianID OR v.VerificationDateTime IS NULL
                 OR res.CaptureDateTime IS NULL OR v.VerificationDateTime < res.CaptureDateTime))
        """;

    public async Task<bool> Release(int doctorId, int requestId, string note, string notification)
    {
        using var db = Connection();
        await db.OpenAsync();
        using var tx = db.BeginTransaction(IsolationLevel.Serializable);
        if (await db.ExecuteScalarAsync<int>(Ready, new { doctorId, requestId }, tx) != 1) return false;
        var changed = await db.ExecuteAsync("""
            UPDATE dbo.TestRequest SET Status = 'Released by doctor', DoctorReleaseNote = @note,
                ReleaseDate = SYSDATETIME(), ReleasedByDoctorID = @doctorId
            WHERE TestRequestID = @requestId AND DoctorID = @doctorId AND Status = 'Completed'
            """, new { doctorId, requestId, note }, tx);
        if (changed != 1) return false;
        await Queue(db, tx, notification, "Release", requestId);
        tx.Commit();
        return true;
    }

    public async Task<bool> EmailResults(int doctorId, int requestId, string notification)
    {
        using var db = Connection();
        await db.OpenAsync();
        using var tx = db.BeginTransaction(IsolationLevel.Serializable);
        if (await db.ExecuteScalarAsync<int>(Ready + " AND r.Status = 'Released by doctor'", new { doctorId, requestId }, tx) != 1) return false;
        // Avoid duplicate clicks queuing the same request repeatedly while delivery is pending.
        if (await db.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM dbo.DoctorEmailQueue WHERE TestRequestID = @requestId AND Kind = 'ResultEmail' AND SentAt IS NULL", new { requestId }, tx) == 0)
            await Queue(db, tx, notification, "ResultEmail", requestId);
        tx.Commit();
        return true;
    }

    public async Task<DoctorEmailJob?> ClaimEmail()
    {
        using var db = Connection();
        return await db.QuerySingleOrDefaultAsync<DoctorEmailJob>("""
            ;WITH pending AS (SELECT TOP (1) * FROM dbo.DoctorEmailQueue WITH (UPDLOCK, READPAST, ROWLOCK)
                WHERE SentAt IS NULL AND NextAttemptAt <= SYSUTCDATETIME()
                  AND (LockedUntil IS NULL OR LockedUntil < SYSUTCDATETIME()) ORDER BY Id)
            UPDATE pending SET LeaseId = NEWID(), LockedUntil = DATEADD(MINUTE, 2, SYSUTCDATETIME()), Attempts = Attempts + 1
            OUTPUT INSERTED.Id, INSERTED.Payload, INSERTED.TestRequestID, INSERTED.Kind, INSERTED.Attempts, INSERTED.LeaseId;
            """);
    }

    public async Task CompleteEmail(DoctorEmailJob job, bool delivered)
    {
        using var db = Connection();
        await db.ExecuteAsync("""
            UPDATE dbo.DoctorEmailQueue SET SentAt = CASE WHEN @delivered = 1 THEN SYSUTCDATETIME() ELSE NULL END,
                Payload = CASE WHEN @delivered = 1 THEN NULL ELSE Payload END,
                LockedUntil = NULL, LeaseId = NULL, NextAttemptAt = DATEADD(MINUTE, 5, SYSUTCDATETIME())
            WHERE Id = @Id AND LeaseId = @LeaseId
            """, new { job.Id, job.LeaseId, delivered });
    }

    public async Task<(string Email, string Name, string Reason)?> CancellationRecipient(int requestId)
    {
        using var db = Connection();
        var row = await db.QuerySingleOrDefaultAsync<DoctorRequestRecord>("""
            SELECT u.Email, CONCAT(u.Name, ' ', u.Surname) AS PatientName, r.CancelReason AS CancellationReason
            FROM dbo.TestRequest r INNER JOIN dbo.Doctor d ON d.DoctorID = r.DoctorID
            INNER JOIN dbo.[User] u ON u.UserID = d.UserID
            WHERE r.TestRequestID = @requestId AND r.Status = 'Cancelled'
            """, new { requestId });
        return row == null ? null : (row.Email, row.PatientName, row.CancellationReason);
    }
}
