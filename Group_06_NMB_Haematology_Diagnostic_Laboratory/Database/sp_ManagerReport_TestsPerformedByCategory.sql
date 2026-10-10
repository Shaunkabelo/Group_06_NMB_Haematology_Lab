CREATE OR ALTER PROCEDURE dbo.sp_ManagerReport_TestsPerformedByCategory
    @StartDate datetime2,
    @EndDateExclusive datetime2
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COALESCE(category.CategoryName, 'Uncategorised') AS CategoryName,
        COUNT(*) AS TotalTests
    FROM dbo.LabTest AS labTest
    INNER JOIN dbo.TestRequestTestType AS requestTestType
        ON requestTestType.TestRequestTestTypeID = labTest.TestRequestTestTypeID
    INNER JOIN dbo.TestType AS testType
        ON testType.TestTypeID = requestTestType.TestTypeID
    LEFT JOIN dbo.TestCategory AS category
        ON category.TestCategoryID = testType.TestCategoryID
    WHERE labTest.CompletionDateTime >= @StartDate
      AND labTest.CompletionDateTime < @EndDateExclusive
    GROUP BY category.CategoryName
    ORDER BY TotalTests DESC, CategoryName;
END;
GO
