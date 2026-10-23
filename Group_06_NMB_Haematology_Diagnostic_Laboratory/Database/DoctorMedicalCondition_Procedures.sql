-- Select the application database used by ConnectionStrings:conn before running this file.

SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [dbo].[spMedicalCondition_GetCategories]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT
        cat.CategoryName AS Category
    FROM dbo.MedicalConditionsCategory AS cat
    INNER JOIN dbo.MedicalCondition AS mc
        ON mc.MedicalConditionsCategoryID = cat.MedicalConditionsCategoryID
    WHERE cat.IsActive = 1
      AND mc.IsActive = 1
      AND NULLIF(LTRIM(RTRIM(cat.CategoryName)), N'') IS NOT NULL
    ORDER BY Category;
END;
GO

CREATE OR ALTER PROCEDURE [dbo].[spMedicalCondition_GetByCategory]
    @Category NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        mc.MedicalConditionID,
        mc.ConditionName,
        cat.CategoryName AS Category
    FROM dbo.MedicalCondition AS mc
    INNER JOIN dbo.MedicalConditionsCategory AS cat
        ON cat.MedicalConditionsCategoryID = mc.MedicalConditionsCategoryID
    WHERE cat.CategoryName = @Category
      AND cat.IsActive = 1
      AND mc.IsActive = 1
    ORDER BY mc.ConditionName;
END;
GO

-- Confirm both procedures now return rows in the shape expected by the app.
EXEC dbo.spMedicalCondition_GetCategories;
GO
