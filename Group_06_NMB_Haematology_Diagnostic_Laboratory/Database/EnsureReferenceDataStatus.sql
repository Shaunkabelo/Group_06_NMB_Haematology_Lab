-- Run against the application database if these status columns are missing.
-- Existing records remain active; existing columns and values are preserved.
IF COL_LENGTH('dbo.Allergy', 'IsActive') IS NULL
    ALTER TABLE dbo.Allergy ADD IsActive bit NOT NULL
        CONSTRAINT DF_Allergy_IsActive DEFAULT (1) WITH VALUES;
GO
IF COL_LENGTH('dbo.Medication', 'IsActive') IS NULL
    ALTER TABLE dbo.Medication ADD IsActive bit NOT NULL
        CONSTRAINT DF_Medication_IsActive DEFAULT (1) WITH VALUES;
GO
