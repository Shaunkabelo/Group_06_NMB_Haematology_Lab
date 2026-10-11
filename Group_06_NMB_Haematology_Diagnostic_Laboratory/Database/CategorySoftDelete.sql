SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.AllergyCategory', 'IsActive') IS NULL
    ALTER TABLE dbo.AllergyCategory ADD IsActive bit NOT NULL CONSTRAINT DF_AllergyCategory_IsActive DEFAULT (1) WITH VALUES;

IF COL_LENGTH('dbo.MedicationCategory', 'IsActive') IS NULL
    ALTER TABLE dbo.MedicationCategory ADD IsActive bit NOT NULL CONSTRAINT DF_MedicationCategory_IsActive DEFAULT (1) WITH VALUES;

IF COL_LENGTH('dbo.MedicalConditionsCategory', 'IsActive') IS NULL
    ALTER TABLE dbo.MedicalConditionsCategory ADD IsActive bit NOT NULL CONSTRAINT DF_MedicalConditionsCategory_IsActive DEFAULT (1) WITH VALUES;

COMMIT TRANSACTION;