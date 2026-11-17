/*
    Address lookup seed for user create/edit dropdowns.

    Loads active Province, TownCity and Suburb values used by the structured
    address controls. It does not create, update or delete user accounts.

    Safe usage:
      1. Select GRP-04-06.
      2. Run with @Apply = 0 to preview the lookup data.
      3. Change only @Apply to 1 and rerun to save the records.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ExpectedDatabase sysname = N'GRP-04-06';
DECLARE @Apply bit = 0;

IF DB_NAME() <> @ExpectedDatabase OR DB_ID() <= 4
    THROW 51000, 'Select GRP-04-06 before running this address lookup seed.', 1;

IF @@TRANCOUNT <> 0
    THROW 51001, 'Run this script without an existing transaction.', 1;

IF OBJECT_ID(N'dbo.Province', N'U') IS NULL
   OR OBJECT_ID(N'dbo.TownCity', N'U') IS NULL
   OR OBJECT_ID(N'dbo.Suburb', N'U') IS NULL
   OR OBJECT_ID(N'dbo.UserAddress', N'U') IS NULL
    THROW 51002, 'Run Structured_Staff_Addresses.sql first to create the address tables.', 1;

DECLARE @Provinces TABLE (ProvinceName NVARCHAR(100) NOT NULL PRIMARY KEY);
INSERT @Provinces (ProvinceName) VALUES
    (N'Eastern Cape'), (N'Free State'), (N'Gauteng'), (N'KwaZulu-Natal'),
    (N'Limpopo'), (N'Mpumalanga'), (N'Northern Cape'), (N'North West'),
    (N'Western Cape');

DECLARE @Towns TABLE
(
    ProvinceName NVARCHAR(100) NOT NULL,
    TownCityName NVARCHAR(100) NOT NULL,
    PRIMARY KEY (ProvinceName, TownCityName)
);
INSERT @Towns (ProvinceName, TownCityName) VALUES
    (N'Eastern Cape', N'Gqeberha'),
    (N'Eastern Cape', N'Kariega'),
    (N'Eastern Cape', N'Despatch'),
    (N'Eastern Cape', N'East London'),
    (N'Eastern Cape', N'Makhanda'),
    (N'Free State', N'Bloemfontein'),
    (N'Gauteng', N'Johannesburg'),
    (N'KwaZulu-Natal', N'Durban'),
    (N'Limpopo', N'Polokwane'),
    (N'Mpumalanga', N'Mbombela'),
    (N'Northern Cape', N'Kimberley'),
    (N'North West', N'Mahikeng'),
    (N'Western Cape', N'Cape Town');

DECLARE @Suburbs TABLE
(
    ProvinceName NVARCHAR(100) NOT NULL,
    TownCityName NVARCHAR(100) NOT NULL,
    SuburbName NVARCHAR(100) NOT NULL,
    AreaCode NVARCHAR(10) NOT NULL,
    PRIMARY KEY (ProvinceName, TownCityName, SuburbName)
);
INSERT @Suburbs (ProvinceName, TownCityName, SuburbName, AreaCode) VALUES
    (N'Eastern Cape', N'Gqeberha', N'Central', N'6001'),
    (N'Eastern Cape', N'Gqeberha', N'Walmer', N'6070'),
    (N'Eastern Cape', N'Gqeberha', N'Summerstrand', N'6001'),
    (N'Eastern Cape', N'Kariega', N'Uitenhage Central', N'6229'),
    (N'Eastern Cape', N'Despatch', N'Despatch Central', N'6220'),
    (N'Eastern Cape', N'East London', N'Beacon Bay', N'5241'),
    (N'Eastern Cape', N'East London', N'East London Central', N'5201'),
    (N'Eastern Cape', N'Makhanda', N'Makhanda Central', N'6139'),
    (N'Free State', N'Bloemfontein', N'Bloemfontein Central', N'9301'),
    (N'Gauteng', N'Johannesburg', N'Johannesburg Central', N'2001'),
    (N'KwaZulu-Natal', N'Durban', N'Durban Central', N'4001'),
    (N'Limpopo', N'Polokwane', N'Polokwane Central', N'0700'),
    (N'Mpumalanga', N'Mbombela', N'Mbombela Central', N'1201'),
    (N'Northern Cape', N'Kimberley', N'Kimberley Central', N'8301'),
    (N'North West', N'Mahikeng', N'Mahikeng Central', N'2745'),
    (N'Western Cape', N'Cape Town', N'Cape Town City Centre', N'8001');

BEGIN TRY
    BEGIN TRANSACTION;

    INSERT dbo.Province (ProvinceName)
    SELECT source.ProvinceName
    FROM @Provinces AS source
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.Province AS target
        WHERE target.ProvinceName = source.ProvinceName
    );

    UPDATE target
    SET IsActive = 1
    FROM dbo.Province AS target
    INNER JOIN @Provinces AS source ON source.ProvinceName = target.ProvinceName
    WHERE target.IsActive = 0;

    INSERT dbo.TownCity (ProvinceID, TownCityName)
    SELECT province.ProvinceID, source.TownCityName
    FROM @Towns AS source
    INNER JOIN dbo.Province AS province ON province.ProvinceName = source.ProvinceName
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.TownCity AS target
        WHERE target.ProvinceID = province.ProvinceID
          AND target.TownCityName = source.TownCityName
    );

    UPDATE target
    SET IsActive = 1
    FROM dbo.TownCity AS target
    INNER JOIN dbo.Province AS province ON province.ProvinceID = target.ProvinceID
    INNER JOIN @Towns AS source
        ON source.ProvinceName = province.ProvinceName
       AND source.TownCityName = target.TownCityName
    WHERE target.IsActive = 0;

    INSERT dbo.Suburb (TownCityID, SuburbName, AreaCode)
    SELECT town.TownCityID, source.SuburbName, source.AreaCode
    FROM @Suburbs AS source
    INNER JOIN dbo.Province AS province ON province.ProvinceName = source.ProvinceName
    INNER JOIN dbo.TownCity AS town
        ON town.ProvinceID = province.ProvinceID
       AND town.TownCityName = source.TownCityName
    WHERE NOT EXISTS (
        SELECT 1
        FROM dbo.Suburb AS target
        WHERE target.TownCityID = town.TownCityID
          AND target.SuburbName = source.SuburbName
    );

    UPDATE target
    SET IsActive = 1,
        AreaCode = source.AreaCode
    FROM dbo.Suburb AS target
    INNER JOIN dbo.TownCity AS town ON town.TownCityID = target.TownCityID
    INNER JOIN dbo.Province AS province ON province.ProvinceID = town.ProvinceID
    INNER JOIN @Suburbs AS source
        ON source.ProvinceName = province.ProvinceName
       AND source.TownCityName = town.TownCityName
       AND source.SuburbName = target.SuburbName
    WHERE target.IsActive = 0
       OR target.AreaCode <> source.AreaCode;

    EXEC sys.sp_executesql N'
CREATE OR ALTER PROCEDURE dbo.sp_Address_GetProvinces
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ProvinceID, ProvinceName
    FROM dbo.Province
    WHERE IsActive = 1
    ORDER BY ProvinceName;
END;';

    EXEC sys.sp_executesql N'
CREATE OR ALTER PROCEDURE dbo.sp_Address_GetTownCitiesByProvince
    @ProvinceID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TownCityID, ProvinceID, TownCityName
    FROM dbo.TownCity
    WHERE ProvinceID = @ProvinceID
      AND IsActive = 1
    ORDER BY TownCityName;
END;';

    EXEC sys.sp_executesql N'
CREATE OR ALTER PROCEDURE dbo.sp_Address_GetSuburbsByTownCity
    @TownCityID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SuburbID, TownCityID, SuburbName, AreaCode
    FROM dbo.Suburb
    WHERE TownCityID = @TownCityID
      AND IsActive = 1
    ORDER BY SuburbName;
END;';

    SELECT N'Province' AS LookupType, COUNT(*) AS ActiveRows
    FROM dbo.Province WHERE IsActive = 1
    UNION ALL
    SELECT N'Town/City', COUNT(*)
    FROM dbo.TownCity WHERE IsActive = 1
    UNION ALL
    SELECT N'Suburb', COUNT(*)
    FROM dbo.Suburb WHERE IsActive = 1;

    IF @Apply = 1
    BEGIN
        COMMIT TRANSACTION;
        PRINT 'Address lookups and dropdown procedures were installed.';
    END
    ELSE
    BEGIN
        ROLLBACK TRANSACTION;
        PRINT 'Preview only. No address lookup data was saved. Set @Apply = 1 and rerun the whole script to install.';
    END;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
