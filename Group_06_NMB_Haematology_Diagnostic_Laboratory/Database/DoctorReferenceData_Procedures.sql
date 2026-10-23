-- Run in the application database before publishing the Doctor patient dropdown changes.
-- These result columns match the Allergy and Medication models used by DoctorRepository.
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE dbo.spAllergy_GetCategories
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT ac.CategoryName AS Category
    FROM dbo.AllergyCategory AS ac
    INNER JOIN dbo.Allergy AS a ON a.AllergyCategoryID = ac.AllergyCategoryID
    WHERE ac.IsActive = 1
      AND a.IsActive = 1
      AND NULLIF(LTRIM(RTRIM(ac.CategoryName)), N'') IS NOT NULL
    ORDER BY Category;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spAllergy_GetByCategory
    @Category NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT a.AllergyID, a.AllergyName, ac.CategoryName AS Category, a.Description
    FROM dbo.Allergy AS a
    INNER JOIN dbo.AllergyCategory AS ac ON ac.AllergyCategoryID = a.AllergyCategoryID
    WHERE ac.CategoryName = @Category
      AND ac.IsActive = 1
      AND a.IsActive = 1
    ORDER BY a.AllergyName;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spMedication_GetCategories
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT mc.CategoryName AS Category
    FROM dbo.MedicationCategory AS mc
    INNER JOIN dbo.Medication AS m ON m.MedicationCategoryID = mc.MedicationCategoryID
    WHERE mc.IsActive = 1
      AND m.IsActive = 1
      AND NULLIF(LTRIM(RTRIM(mc.CategoryName)), N'') IS NOT NULL
    ORDER BY Category;
END;
GO

CREATE OR ALTER PROCEDURE dbo.spMedication_GetByCategory
    @Category NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT m.MedicationID, m.MedicationName, mc.CategoryName AS Category
    FROM dbo.Medication AS m
    INNER JOIN dbo.MedicationCategory AS mc ON mc.MedicationCategoryID = m.MedicationCategoryID
    WHERE mc.CategoryName = @Category
      AND mc.IsActive = 1
      AND m.IsActive = 1
    ORDER BY m.MedicationName;
END;
GO
