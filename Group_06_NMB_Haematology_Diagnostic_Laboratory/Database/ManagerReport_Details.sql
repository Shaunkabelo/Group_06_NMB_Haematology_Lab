/* Run in GRP-04-06 after the reporting tables exist. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
CREATE OR ALTER PROCEDURE dbo.sp_ManagerReport_TestsPerformedDetail
    @StartDate datetime2,
    @EndDateExclusive datetime2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        testType.TestTypeID,
        COALESCE(category.CategoryName, N'Uncategorised') AS CategoryName,
        testType.TestName,
        labTest.CompletionDateTime
    FROM dbo.LabTest AS labTest
    INNER JOIN dbo.TestRequestTestType AS requestTestType
        ON requestTestType.TestRequestTestTypeID = labTest.TestRequestTestTypeID
    INNER JOIN dbo.TestType AS testType
        ON testType.TestTypeID = requestTestType.TestTypeID
    LEFT JOIN dbo.TestCategory AS category
        ON category.TestCategoryID = testType.TestCategoryID
    WHERE labTest.CompletionDateTime >= @StartDate
      AND labTest.CompletionDateTime < @EndDateExclusive
    ORDER BY COALESCE(category.CategoryName, N'Uncategorised'), testType.TestName, labTest.CompletionDateTime;
END;
GO
