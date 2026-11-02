-- Review and run against the application database after Authentication_PasswordRecovery.sql.
-- Additive only: no existing tables or procedures are replaced. Do not run against production without approval.
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.DoctorEmailQueue', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DoctorEmailQueue
    (
        Id BIGINT IDENTITY PRIMARY KEY,
        TestRequestID INT NULL REFERENCES dbo.TestRequest(TestRequestID),
        Kind NVARCHAR(30) NOT NULL,
        Payload NVARCHAR(MAX) NULL, -- ASP.NET Data Protection encrypted email (including any attachment).
        CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        SentAt DATETIME2 NULL,
        NextAttemptAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
        Attempts INT NOT NULL DEFAULT 0,
        LeaseId UNIQUEIDENTIFIER NULL,
        LockedUntil DATETIME2 NULL
    );
    CREATE INDEX IX_DoctorEmailQueue_Pending ON dbo.DoctorEmailQueue(SentAt, NextAttemptAt, LockedUntil);
    CREATE UNIQUE INDEX UX_DoctorEmailQueue_Cancellation
        ON dbo.DoctorEmailQueue(TestRequestID) WHERE Kind = N'Cancellation';
END;
COMMIT;
GO

-- Queue future technician cancellations from any subsystem without changing its controller.
CREATE OR ALTER TRIGGER dbo.tr_DoctorNotifyCancellation ON dbo.TestRequest
AFTER UPDATE AS
BEGIN
    SET NOCOUNT ON;
    INSERT dbo.DoctorEmailQueue(TestRequestID, Kind)
    SELECT i.TestRequestID, N'Cancellation'
    FROM inserted i INNER JOIN deleted d ON d.TestRequestID = i.TestRequestID
    WHERE i.Status = N'Cancelled' AND d.Status <> N'Cancelled'
      AND NOT EXISTS (SELECT 1 FROM dbo.DoctorEmailQueue q
                      WHERE q.TestRequestID = i.TestRequestID AND q.Kind = N'Cancellation');
END;
GO
