/*
    Hosted database deployment: Doctor test-request cancellation.

    Safe use:
      1. In SSMS select GRP-04-06.
      2. Run with @Apply = 0. It validates and rolls back.
      3. Change only @Apply to 1 and run the whole script again to install.

    It changes only dbo.spTestRequest_CancelByDoctor. No request data is
    changed during deployment. Cancellation itself remains restricted to the
    authenticated doctor and requests that are still Submitted/Samples Received.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @ExpectedDatabase sysname = N'GRP-04-06';
DECLARE @Apply bit = 0;

IF DB_NAME() <> @ExpectedDatabase OR DB_ID() <= 4
    THROW 51000, 'Select the hosted application database and set @ExpectedDatabase correctly.', 1;
IF @@TRANCOUNT <> 0
    THROW 51001, 'Run this script without an existing transaction.', 1;
IF OBJECT_ID(N'dbo.TestRequest', N'U') IS NULL OR OBJECT_ID(N'dbo.Doctor', N'U') IS NULL
    THROW 51002, 'Required Doctor and TestRequest tables are missing.', 1;
IF COL_LENGTH(N'dbo.TestRequest', N'TestRequestID') IS NULL
   OR COL_LENGTH(N'dbo.TestRequest', N'DoctorID') IS NULL
   OR COL_LENGTH(N'dbo.TestRequest', N'Status') IS NULL
   OR COL_LENGTH(N'dbo.TestRequest', N'CancelledBy') IS NULL
   OR COL_LENGTH(N'dbo.TestRequest', N'CancelledDate') IS NULL
   OR COL_LENGTH(N'dbo.TestRequest', N'CancelReason') IS NULL
    THROW 51003, 'The TestRequest cancellation columns are missing. Run the Doctor workflow schema deployment first.', 1;
IF COL_LENGTH(N'dbo.TestRequest', N'CancelledBy') < 20
    THROW 51004, 'TestRequest.CancelledBy is too short for the cancellation audit value.', 1;
IF COL_LENGTH(N'dbo.TestRequest', N'CancelReason') <> -1
   AND COL_LENGTH(N'dbo.TestRequest', N'CancelReason') < 1000
    THROW 51005, 'TestRequest.CancelReason must support 500 Unicode characters.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    EXEC sys.sp_executesql N'
CREATE OR ALTER PROCEDURE dbo.spTestRequest_CancelByDoctor
    @TestRequestID INT,
    @DoctorID INT,
    @CancelledBy NVARCHAR(200),
    @CancelReason NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @TestRequestID IS NULL OR @TestRequestID <= 0
       OR @DoctorID IS NULL OR @DoctorID <= 0
        THROW 51100, ''A valid request and doctor are required.'', 1;
    SET @CancelReason = LTRIM(RTRIM(@CancelReason));
    SET @CancelledBy = LTRIM(RTRIM(@CancelledBy));
    IF @CancelReason IS NULL OR LEN(@CancelReason) = 0
        THROW 51101, ''A cancellation reason is required.'', 1;
    IF @CancelledBy IS NULL OR LEN(@CancelledBy) = 0
        THROW 51102, ''The cancellation user is required.'', 1;

    UPDATE dbo.TestRequest
    SET Status = N''Cancelled'',
        CancelledBy = @CancelledBy,
        CancelledDate = SYSUTCDATETIME(),
        CancelReason = @CancelReason
    WHERE TestRequestID = @TestRequestID
      AND DoctorID = @DoctorID
      AND Status IN (N''Submitted'', N''Samples Received'', N''Sample(s) received'');

    IF @@ROWCOUNT = 1
    BEGIN
        SELECT CAST(1 AS INT) AS Result;
        RETURN;
    END;

    IF EXISTS (SELECT 1 FROM dbo.TestRequest WHERE TestRequestID = @TestRequestID AND DoctorID = @DoctorID)
        SELECT CAST(2 AS INT) AS Result;
    ELSE
        SELECT CAST(0 AS INT) AS Result;
END;';

    IF @Apply = 1
    BEGIN
        COMMIT TRANSACTION;
        PRINT 'Doctor cancellation procedure installed on the hosted database.';
    END
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT 'Preview passed; no changes were saved. Set @Apply = 1 and rerun to install.';
    END;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
