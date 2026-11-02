-- Extend the existing procedures; existing technician-page calls remain valid.
CREATE OR ALTER PROCEDURE dbo.sp_LabTechnicianTestType_GetByTechnician
    @LabTechnicianID INT = NULL, @TestTypeID INT = NULL, @TestCategoryID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SELECT a.LabTechnicianTestTypeID, a.LabTechnicianID, a.TestTypeID,
           t.TestName, t.TestCategoryID, c.CategoryName
    FROM dbo.LabTechnicianTestType a
    JOIN dbo.TestType t ON t.TestTypeID=a.TestTypeID
    JOIN dbo.TestCategory c ON c.TestCategoryID=t.TestCategoryID
    WHERE (@LabTechnicianID IS NULL OR a.LabTechnicianID=@LabTechnicianID)
      AND (@TestTypeID IS NULL OR a.TestTypeID=@TestTypeID)
      AND (@TestCategoryID IS NULL OR t.TestCategoryID=@TestCategoryID)
    ORDER BY c.CategoryName,t.TestName,a.LabTechnicianID;
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabTechnicianTestType_Replace
    @LabTechnicianID INT = NULL, @TestTypeIDs NVARCHAR(MAX) = N'',
    @TestTypeID INT = NULL, @TechnicianIDs NVARCHAR(MAX) = N'',
    @ExpectedTechnicianIDs NVARCHAR(MAX) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    IF (@LabTechnicianID IS NULL AND @TestTypeID IS NULL)
        OR (@LabTechnicianID IS NOT NULL AND @TestTypeID IS NOT NULL)
        THROW 50100,'Specify one technician or one test type.',1;
    BEGIN TRANSACTION;
    BEGIN TRY
        DECLARE @LockResult INT;
        EXEC @LockResult=sys.sp_getapplock @Resource='LabTechnicianTestTypeAssignments',
            @LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000;
        IF @LockResult<0 THROW 50100,'Assignments are busy. Please try again.',1;
        DECLARE @IDs NVARCHAR(MAX)=CASE WHEN @TestTypeID IS NULL THEN @TestTypeIDs ELSE @TechnicianIDs END;
        IF EXISTS(SELECT 1 FROM STRING_SPLIT(@IDs,',') WHERE NULLIF(LTRIM(RTRIM(value)),'') IS NOT NULL AND (TRY_CONVERT(INT,value) IS NULL OR TRY_CONVERT(INT,value)<=0))
            THROW 50101,'Invalid assignment selection.',1;
        DECLARE @Requested TABLE(ID INT PRIMARY KEY);
        INSERT @Requested SELECT DISTINCT CONVERT(INT,value) FROM STRING_SPLIT(@IDs,',') WHERE NULLIF(LTRIM(RTRIM(value)),'') IS NOT NULL;
        IF @TestTypeID IS NULL
        BEGIN
            IF NOT EXISTS(SELECT 1 FROM dbo.LabTechnician l JOIN dbo.[User] u ON u.UserID=l.UserID
                WHERE l.LabTechnicianID=@LabTechnicianID AND u.IsActive=1)
                THROW 50100,'Active technician was not found.',1;
            IF NOT EXISTS(SELECT 1 FROM @Requested)
                THROW 50102,'Assign at least one active test type to the technician.',1;
            IF EXISTS(SELECT 1 FROM @Requested r LEFT JOIN dbo.TestType t ON t.TestTypeID=r.ID AND t.IsActive=1 WHERE t.TestTypeID IS NULL)
                THROW 50101,'One or more selected test types are not active.',1;
            DELETE a FROM dbo.LabTechnicianTestType a JOIN dbo.TestType t ON t.TestTypeID=a.TestTypeID
            WHERE a.LabTechnicianID=@LabTechnicianID AND t.IsActive=1 AND NOT EXISTS(SELECT 1 FROM @Requested WHERE ID=a.TestTypeID);
            INSERT dbo.LabTechnicianTestType(LabTechnicianID,TestTypeID)
            SELECT @LabTechnicianID,ID FROM @Requested r WHERE NOT EXISTS(
                SELECT 1 FROM dbo.LabTechnicianTestType WHERE LabTechnicianID=@LabTechnicianID AND TestTypeID=r.ID);
        END
        ELSE
        BEGIN
            IF NOT EXISTS(SELECT 1 FROM dbo.TestType WHERE TestTypeID=@TestTypeID AND IsActive=1)
                THROW 50100,'Active test type was not found.',1;
            IF EXISTS(SELECT 1 FROM @Requested r LEFT JOIN dbo.LabTechnician l ON l.LabTechnicianID=r.ID
                LEFT JOIN dbo.[User] u ON u.UserID=l.UserID AND u.IsActive=1 WHERE u.UserID IS NULL)
                THROW 50101,'One or more selected technicians are not active.',1;
            DECLARE @Current TABLE(ID INT PRIMARY KEY);
            INSERT @Current SELECT DISTINCT a.LabTechnicianID FROM dbo.LabTechnicianTestType a
                JOIN dbo.LabTechnician l ON l.LabTechnicianID=a.LabTechnicianID
                JOIN dbo.[User] u ON u.UserID=l.UserID
                WHERE a.TestTypeID=@TestTypeID AND u.IsActive=1;
            IF @ExpectedTechnicianIDs IS NOT NULL
            BEGIN
                DECLARE @Expected TABLE(ID INT PRIMARY KEY);
                IF EXISTS(SELECT 1 FROM STRING_SPLIT(@ExpectedTechnicianIDs,',') WHERE NULLIF(LTRIM(RTRIM(value)),'') IS NOT NULL AND TRY_CONVERT(INT,value) IS NULL)
                    THROW 50101,'Invalid previous assignment selection.',1;
                INSERT @Expected SELECT DISTINCT CONVERT(INT,value) FROM STRING_SPLIT(@ExpectedTechnicianIDs,',') WHERE NULLIF(LTRIM(RTRIM(value)),'') IS NOT NULL;
                IF EXISTS(SELECT ID FROM @Current EXCEPT SELECT ID FROM @Expected)
                    OR EXISTS(SELECT ID FROM @Expected EXCEPT SELECT ID FROM @Current)
                    THROW 50103,'Assignments changed since this page was loaded. Review the refreshed list and try again.',1;
            END
            IF EXISTS(SELECT 1 FROM @Current c WHERE NOT EXISTS(SELECT 1 FROM @Requested WHERE ID=c.ID)
                AND NOT EXISTS(SELECT 1 FROM dbo.LabTechnicianTestType a JOIN dbo.TestType t ON t.TestTypeID=a.TestTypeID
                    WHERE a.LabTechnicianID=c.ID AND a.TestTypeID<>@TestTypeID AND t.IsActive=1))
                THROW 50102,'A technician would be left without an active test type. Assign them another test type before removing them.',1;
            -- Preserve inactive technicians' existing links; only active selections are edited.
            DELETE a FROM dbo.LabTechnicianTestType a JOIN @Current c ON c.ID=a.LabTechnicianID
            WHERE a.TestTypeID=@TestTypeID AND NOT EXISTS(SELECT 1 FROM @Requested WHERE ID=c.ID);
            INSERT dbo.LabTechnicianTestType(LabTechnicianID,TestTypeID)
            SELECT ID,@TestTypeID FROM @Requested r WHERE NOT EXISTS(
                SELECT 1 FROM dbo.LabTechnicianTestType WHERE TestTypeID=@TestTypeID AND LabTechnicianID=r.ID);
        END
        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT>0 ROLLBACK;
        THROW;
    END CATCH
END
GO

