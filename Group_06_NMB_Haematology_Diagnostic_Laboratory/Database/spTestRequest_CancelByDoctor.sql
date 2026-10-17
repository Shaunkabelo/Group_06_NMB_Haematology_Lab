-- Select the application database (for example [GRP-04-06]) before running this script.
-- The procedure records the authenticated doctor's username, SQL Server date/time, and reason.
CREATE OR ALTER PROCEDURE dbo.spTestRequest_CancelByDoctor
    @TestRequestID INT,
    @CancelledBy NVARCHAR(200),
    @CancelReason NVARCHAR(500)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.TestRequest
    SET Status = N'Cancelled',
        CancelledBy = @CancelledBy,
        CancelledDate = SYSDATETIME(),
        CancelReason = @CancelReason
    WHERE TestRequestID = @TestRequestID
      AND DoctorID = @DoctorID
      AND Status IN (N'Submitted', N'Samples Received');

    IF @@ROWCOUNT = 1
    BEGIN
        SELECT 1;
        RETURN;
    END;

    IF EXISTS (
        SELECT 1
        FROM dbo.TestRequest
        WHERE TestRequestID = @TestRequestID AND DoctorID = @DoctorID
    )
        SELECT 2; -- Owned by this doctor, but no longer cancellable.
    ELSE
        SELECT 0; -- Missing request or request belongs to another doctor.
END;
