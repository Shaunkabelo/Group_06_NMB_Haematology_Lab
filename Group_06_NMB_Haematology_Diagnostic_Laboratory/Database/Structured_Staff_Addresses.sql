-- Additive, repeatable migration for structured staff addresses.
-- Run against the application database (local first, then the hosted database).
-- This does not alter or remove existing User.HomeAddress values.
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF OBJECT_ID('dbo.[User]', 'U') IS NULL
    THROW 51000, 'Select the application database containing dbo.User first.', 1;
GO

IF OBJECT_ID('dbo.Province', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Province (
        ProvinceID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Province PRIMARY KEY,
        ProvinceName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Province_IsActive DEFAULT 1,
        CONSTRAINT UQ_Province_ProvinceName UNIQUE (ProvinceName)
    );
END
GO
IF OBJECT_ID('dbo.TownCity', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.TownCity (
        TownCityID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TownCity PRIMARY KEY,
        ProvinceID INT NOT NULL CONSTRAINT FK_TownCity_Province REFERENCES dbo.Province(ProvinceID),
        TownCityName NVARCHAR(100) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_TownCity_IsActive DEFAULT 1,
        CONSTRAINT UQ_TownCity_Province_Name UNIQUE (ProvinceID, TownCityName)
    );
END
GO
IF OBJECT_ID('dbo.Suburb', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Suburb (
        SuburbID INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Suburb PRIMARY KEY,
        TownCityID INT NOT NULL CONSTRAINT FK_Suburb_TownCity REFERENCES dbo.TownCity(TownCityID),
        SuburbName NVARCHAR(100) NOT NULL,
        AreaCode NVARCHAR(10) NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Suburb_IsActive DEFAULT 1,
        CONSTRAINT UQ_Suburb_TownCity_Name UNIQUE (TownCityID, SuburbName)
    );
END
GO
IF OBJECT_ID('dbo.UserAddress', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserAddress (
        UserID INT NOT NULL CONSTRAINT PK_UserAddress PRIMARY KEY
            CONSTRAINT FK_UserAddress_User REFERENCES dbo.[User](UserID),
        AddressLine1 NVARCHAR(150) NOT NULL,
        AddressLine2 NVARCHAR(150) NULL,
        SuburbID INT NOT NULL CONSTRAINT FK_UserAddress_Suburb REFERENCES dbo.Suburb(SuburbID),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_UserAddress_CreatedAt DEFAULT SYSUTCDATETIME(),
        UpdatedAt DATETIME2 NULL
    );
    CREATE INDEX IX_UserAddress_Suburb ON dbo.UserAddress(SuburbID);
END
GO

-- Initial lookup data. More towns and suburbs can be added without application changes.
INSERT dbo.Province (ProvinceName)
SELECT v.ProvinceName FROM (VALUES
    (N'Eastern Cape'), (N'Free State'), (N'Gauteng'), (N'KwaZulu-Natal'),
    (N'Limpopo'), (N'Mpumalanga'), (N'Northern Cape'), (N'North West'), (N'Western Cape')
) v(ProvinceName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Province p WHERE p.ProvinceName = v.ProvinceName);
GO
DECLARE @EasternCapeID INT = (SELECT ProvinceID FROM dbo.Province WHERE ProvinceName = N'Eastern Cape');
INSERT dbo.TownCity (ProvinceID, TownCityName)
SELECT @EasternCapeID, v.TownCityName FROM (VALUES
    (N'Gqeberha'), (N'Kariega'), (N'Despatch'), (N'East London'), (N'Makhanda')
) v(TownCityName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.TownCity t WHERE t.ProvinceID = @EasternCapeID AND t.TownCityName = v.TownCityName);
INSERT dbo.TownCity (ProvinceID, TownCityName)
SELECT p.ProvinceID, v.TownCityName
FROM (VALUES
    (N'Free State', N'Bloemfontein'), (N'Gauteng', N'Johannesburg'), (N'KwaZulu-Natal', N'Durban'),
    (N'Limpopo', N'Polokwane'), (N'Mpumalanga', N'Mbombela'), (N'Northern Cape', N'Kimberley'),
    (N'North West', N'Mahikeng'), (N'Western Cape', N'Cape Town')
) v(ProvinceName, TownCityName)
JOIN dbo.Province p ON p.ProvinceName = v.ProvinceName
WHERE NOT EXISTS (SELECT 1 FROM dbo.TownCity t WHERE t.ProvinceID = p.ProvinceID AND t.TownCityName = v.TownCityName);
GO
DECLARE @GqeberhaID INT = (SELECT TownCityID FROM dbo.TownCity WHERE TownCityName = N'Gqeberha');
DECLARE @KariegaID INT = (SELECT TownCityID FROM dbo.TownCity WHERE TownCityName = N'Kariega');
DECLARE @DespatchID INT = (SELECT TownCityID FROM dbo.TownCity WHERE TownCityName = N'Despatch');
DECLARE @EastLondonID INT = (SELECT TownCityID FROM dbo.TownCity WHERE TownCityName = N'East London');
DECLARE @MakhandaID INT = (SELECT TownCityID FROM dbo.TownCity WHERE TownCityName = N'Makhanda');
INSERT dbo.Suburb (TownCityID, SuburbName, AreaCode)
SELECT v.TownCityID, v.SuburbName, v.AreaCode FROM (VALUES
    (@GqeberhaID, N'Central', N'6001'), (@GqeberhaID, N'Walmer', N'6070'), (@GqeberhaID, N'Summerstrand', N'6001'),
    (@KariegaID, N'Uitenhage Central', N'6229'), (@DespatchID, N'Despatch Central', N'6220'),
    (@EastLondonID, N'Beacon Bay', N'5241'), (@EastLondonID, N'East London Central', N'5201'),
    (@MakhandaID, N'Makhanda Central', N'6139')
) v(TownCityID, SuburbName, AreaCode)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Suburb s WHERE s.TownCityID = v.TownCityID AND s.SuburbName = v.SuburbName);
INSERT dbo.Suburb (TownCityID, SuburbName, AreaCode)
SELECT t.TownCityID, v.SuburbName, v.AreaCode
FROM (VALUES
    (N'Free State', N'Bloemfontein', N'Bloemfontein Central', N'9301'),
    (N'Gauteng', N'Johannesburg', N'Johannesburg Central', N'2001'),
    (N'KwaZulu-Natal', N'Durban', N'Durban Central', N'4001'),
    (N'Limpopo', N'Polokwane', N'Polokwane Central', N'0700'),
    (N'Mpumalanga', N'Mbombela', N'Mbombela Central', N'1201'),
    (N'Northern Cape', N'Kimberley', N'Kimberley Central', N'8301'),
    (N'North West', N'Mahikeng', N'Mahikeng Central', N'2745'),
    (N'Western Cape', N'Cape Town', N'Cape Town City Centre', N'8001')
) v(ProvinceName, TownCityName, SuburbName, AreaCode)
JOIN dbo.Province p ON p.ProvinceName = v.ProvinceName
JOIN dbo.TownCity t ON t.ProvinceID = p.ProvinceID AND t.TownCityName = v.TownCityName
WHERE NOT EXISTS (SELECT 1 FROM dbo.Suburb s WHERE s.TownCityID = t.TownCityID AND s.SuburbName = v.SuburbName);
GO

CREATE OR ALTER PROCEDURE dbo.sp_Address_GetProvinces
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ProvinceID, ProvinceName FROM dbo.Province WHERE IsActive = 1 ORDER BY ProvinceName;
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_Address_GetTownCitiesByProvince @ProvinceID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TownCityID, ProvinceID, TownCityName FROM dbo.TownCity
    WHERE ProvinceID = @ProvinceID AND IsActive = 1 ORDER BY TownCityName;
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_Address_GetSuburbsByTownCity @TownCityID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT SuburbID, TownCityID, SuburbName, AreaCode FROM dbo.Suburb
    WHERE TownCityID = @TownCityID AND IsActive = 1 ORDER BY SuburbName;
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_Address_GetAllUserAddresses
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ua.UserID, ua.AddressLine1, ua.AddressLine2, ua.SuburbID,
           s.TownCityID, t.ProvinceID, s.SuburbName, s.AreaCode, t.TownCityName, p.ProvinceName
    FROM dbo.UserAddress ua
    JOIN dbo.Suburb s ON s.SuburbID = ua.SuburbID
    JOIN dbo.TownCity t ON t.TownCityID = s.TownCityID
    JOIN dbo.Province p ON p.ProvinceID = t.ProvinceID;
END
GO

CREATE OR ALTER PROCEDURE dbo.sp_Doctor_Create
    @Name NVARCHAR(100), @Surname NVARCHAR(100), @Email NVARCHAR(150),
    @PasswordHash NVARCHAR(255), @PasswordSalt NVARCHAR(255), @Cellphone NVARCHAR(20),
    @HomeAddress NVARCHAR(255) = NULL, @SA_ID_Number NVARCHAR(13), @DateOfBirth DATE, @HPCSA_Number NVARCHAR(20),
    @AddressLine1 NVARCHAR(150) = NULL, @AddressLine2 NVARCHAR(150) = NULL, @SuburbID INT = NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON; BEGIN TRANSACTION;
    BEGIN TRY
        IF (@AddressLine1 IS NULL AND @SuburbID IS NOT NULL) OR (@AddressLine1 IS NOT NULL AND @SuburbID IS NULL) THROW 51001, 'Address line 1 and suburb are both required.', 1;
        IF @SuburbID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Suburb WHERE SuburbID = @SuburbID AND IsActive = 1)
            THROW 51002, 'The selected suburb is not active.', 1;
        INSERT dbo.[User] (Name,Surname,Email,PasswordHash,PasswordSalt,Cellphone,HomeAddress,SA_ID_Number,DateOfBirth,Role,IsActive,MustChangePassword)
        VALUES (@Name,@Surname,@Email,@PasswordHash,@PasswordSalt,@Cellphone,@HomeAddress,@SA_ID_Number,@DateOfBirth,'Doctor',1,1);
        DECLARE @UserID INT = CONVERT(INT,SCOPE_IDENTITY());
        INSERT dbo.Doctor(UserID,HPCSA_Number) VALUES (@UserID,@HPCSA_Number);
        IF @SuburbID IS NOT NULL INSERT dbo.UserAddress(UserID,AddressLine1,AddressLine2,SuburbID) VALUES (@UserID,@AddressLine1,NULLIF(@AddressLine2,N''),@SuburbID);
        COMMIT;
    END TRY BEGIN CATCH IF @@TRANCOUNT > 0 ROLLBACK; THROW; END CATCH
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_Doctor_Update
    @DoctorID INT, @Name NVARCHAR(100), @Surname NVARCHAR(100), @Email NVARCHAR(150), @Cellphone NVARCHAR(20),
    @HomeAddress NVARCHAR(255) = NULL, @SA_ID_Number NVARCHAR(13), @DateOfBirth DATE, @HPCSA_Number NVARCHAR(20),
    @AddressLine1 NVARCHAR(150) = NULL, @AddressLine2 NVARCHAR(150) = NULL, @SuburbID INT = NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON; BEGIN TRANSACTION;
    BEGIN TRY
        IF @AddressLine1 IS NULL OR @SuburbID IS NULL THROW 51001, 'A structured address is required.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.Suburb WHERE SuburbID = @SuburbID AND IsActive = 1) THROW 51002, 'The selected suburb is not active.', 1;
        DECLARE @UserID INT = (SELECT UserID FROM dbo.Doctor WHERE DoctorID = @DoctorID);
        IF @UserID IS NULL THROW 51003, 'Doctor was not found.', 1;
        UPDATE dbo.[User] SET Name=@Name,Surname=@Surname,Email=@Email,Cellphone=@Cellphone,HomeAddress=@HomeAddress,SA_ID_Number=@SA_ID_Number,DateOfBirth=@DateOfBirth,UpdatedAt=SYSUTCDATETIME() WHERE UserID=@UserID;
        UPDATE dbo.Doctor SET HPCSA_Number=@HPCSA_Number WHERE DoctorID=@DoctorID;
        MERGE dbo.UserAddress AS target USING (SELECT @UserID UserID) AS source ON target.UserID=source.UserID
        WHEN MATCHED THEN UPDATE SET AddressLine1=@AddressLine1,AddressLine2=NULLIF(@AddressLine2,N''),SuburbID=@SuburbID,UpdatedAt=SYSUTCDATETIME()
        WHEN NOT MATCHED THEN INSERT(UserID,AddressLine1,AddressLine2,SuburbID) VALUES(@UserID,@AddressLine1,NULLIF(@AddressLine2,N''),@SuburbID);
        COMMIT;
    END TRY BEGIN CATCH IF @@TRANCOUNT > 0 ROLLBACK; THROW; END CATCH
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabManager_Create
    @Name NVARCHAR(100), @Surname NVARCHAR(100), @Email NVARCHAR(150),
    @PasswordHash NVARCHAR(255), @PasswordSalt NVARCHAR(255), @Cellphone NVARCHAR(20),
    @HomeAddress NVARCHAR(255) = NULL, @SA_ID_Number NVARCHAR(13), @DateOfBirth DATE,
    @AddressLine1 NVARCHAR(150) = NULL, @AddressLine2 NVARCHAR(150) = NULL, @SuburbID INT = NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON; BEGIN TRANSACTION;
    BEGIN TRY
        IF (@AddressLine1 IS NULL AND @SuburbID IS NOT NULL) OR (@AddressLine1 IS NOT NULL AND @SuburbID IS NULL) THROW 51001, 'Address line 1 and suburb are both required.', 1;
        IF @SuburbID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Suburb WHERE SuburbID=@SuburbID AND IsActive=1) THROW 51002, 'The selected suburb is not active.', 1;
        INSERT dbo.[User] (Name,Surname,Email,PasswordHash,PasswordSalt,Cellphone,HomeAddress,SA_ID_Number,DateOfBirth,Role,IsActive,MustChangePassword)
        VALUES (@Name,@Surname,@Email,@PasswordHash,@PasswordSalt,@Cellphone,@HomeAddress,@SA_ID_Number,@DateOfBirth,'LabManager',1,1);
        DECLARE @UserID INT=CONVERT(INT,SCOPE_IDENTITY());
        INSERT dbo.LabManager(UserID) VALUES(@UserID);
        IF @SuburbID IS NOT NULL INSERT dbo.UserAddress(UserID,AddressLine1,AddressLine2,SuburbID) VALUES(@UserID,@AddressLine1,NULLIF(@AddressLine2,N''),@SuburbID);
        COMMIT;
    END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabManager_Update
    @LabManagerID INT, @Name NVARCHAR(100), @Surname NVARCHAR(100), @Email NVARCHAR(150), @Cellphone NVARCHAR(20),
    @HomeAddress NVARCHAR(255) = NULL, @SA_ID_Number NVARCHAR(13), @DateOfBirth DATE,
    @AddressLine1 NVARCHAR(150) = NULL, @AddressLine2 NVARCHAR(150) = NULL, @SuburbID INT = NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON; BEGIN TRANSACTION;
    BEGIN TRY
        IF @AddressLine1 IS NULL OR @SuburbID IS NULL THROW 51001, 'A structured address is required.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.Suburb WHERE SuburbID=@SuburbID AND IsActive=1) THROW 51002, 'The selected suburb is not active.', 1;
        DECLARE @UserID INT=(SELECT UserID FROM dbo.LabManager WHERE LabManagerID=@LabManagerID);
        IF @UserID IS NULL THROW 51003, 'Lab manager was not found.', 1;
        UPDATE dbo.[User] SET Name=@Name,Surname=@Surname,Email=@Email,Cellphone=@Cellphone,HomeAddress=@HomeAddress,SA_ID_Number=@SA_ID_Number,DateOfBirth=@DateOfBirth,UpdatedAt=SYSUTCDATETIME() WHERE UserID=@UserID;
        MERGE dbo.UserAddress AS target USING (SELECT @UserID UserID) AS source ON target.UserID=source.UserID
        WHEN MATCHED THEN UPDATE SET AddressLine1=@AddressLine1,AddressLine2=NULLIF(@AddressLine2,N''),SuburbID=@SuburbID,UpdatedAt=SYSUTCDATETIME()
        WHEN NOT MATCHED THEN INSERT(UserID,AddressLine1,AddressLine2,SuburbID) VALUES(@UserID,@AddressLine1,NULLIF(@AddressLine2,N''),@SuburbID);
        COMMIT;
    END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabTechnician_Create
    @Name NVARCHAR(100), @Surname NVARCHAR(100), @Email NVARCHAR(150),
    @PasswordHash NVARCHAR(255), @PasswordSalt NVARCHAR(255), @Cellphone NVARCHAR(20),
    @HomeAddress NVARCHAR(255) = NULL, @SA_ID_Number NVARCHAR(13), @DateOfBirth DATE,
    @EmployeeNumber NVARCHAR(50) = NULL, @AddressLine1 NVARCHAR(150) = NULL, @AddressLine2 NVARCHAR(150) = NULL, @SuburbID INT = NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON; BEGIN TRANSACTION;
    BEGIN TRY
        IF (@AddressLine1 IS NULL AND @SuburbID IS NOT NULL) OR (@AddressLine1 IS NOT NULL AND @SuburbID IS NULL) THROW 51001, 'Address line 1 and suburb are both required.', 1;
        IF @SuburbID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Suburb WHERE SuburbID=@SuburbID AND IsActive=1) THROW 51002, 'The selected suburb is not active.', 1;
        INSERT dbo.[User] (Name,Surname,Email,PasswordHash,PasswordSalt,Cellphone,HomeAddress,SA_ID_Number,DateOfBirth,Role,IsActive,MustChangePassword)
        VALUES(@Name,@Surname,@Email,@PasswordHash,@PasswordSalt,@Cellphone,@HomeAddress,@SA_ID_Number,@DateOfBirth,'LabTechnician',1,1);
        DECLARE @UserID INT=CONVERT(INT,SCOPE_IDENTITY());
        INSERT dbo.LabTechnician(UserID,EmployeeNumber) VALUES(@UserID,COALESCE(NULLIF(LTRIM(RTRIM(@EmployeeNumber)),N''),CONCAT(N'EMP',@UserID)));
        DECLARE @LabTechnicianID INT=CONVERT(INT,SCOPE_IDENTITY());
        IF @SuburbID IS NOT NULL INSERT dbo.UserAddress(UserID,AddressLine1,AddressLine2,SuburbID) VALUES(@UserID,@AddressLine1,NULLIF(@AddressLine2,N''),@SuburbID);
        COMMIT; SELECT @LabTechnicianID;
    END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END
GO
CREATE OR ALTER PROCEDURE dbo.sp_LabTechnician_Update
    @LabTechnicianID INT, @Name NVARCHAR(100), @Surname NVARCHAR(100), @Email NVARCHAR(150), @Cellphone NVARCHAR(20),
    @HomeAddress NVARCHAR(255) = NULL, @SA_ID_Number NVARCHAR(13), @DateOfBirth DATE, @EmployeeNumber NVARCHAR(50),
    @AddressLine1 NVARCHAR(150) = NULL, @AddressLine2 NVARCHAR(150) = NULL, @SuburbID INT = NULL
AS
BEGIN
    SET NOCOUNT ON; SET XACT_ABORT ON; BEGIN TRANSACTION;
    BEGIN TRY
        IF @AddressLine1 IS NULL OR @SuburbID IS NULL THROW 51001, 'A structured address is required.', 1;
        IF NOT EXISTS (SELECT 1 FROM dbo.Suburb WHERE SuburbID=@SuburbID AND IsActive=1) THROW 51002, 'The selected suburb is not active.', 1;
        DECLARE @UserID INT=(SELECT UserID FROM dbo.LabTechnician WHERE LabTechnicianID=@LabTechnicianID);
        IF @UserID IS NULL THROW 51003, 'Lab technician was not found.', 1;
        UPDATE dbo.[User] SET Name=@Name,Surname=@Surname,Email=@Email,Cellphone=@Cellphone,HomeAddress=@HomeAddress,SA_ID_Number=@SA_ID_Number,DateOfBirth=@DateOfBirth,UpdatedAt=SYSUTCDATETIME() WHERE UserID=@UserID;
        UPDATE dbo.LabTechnician SET EmployeeNumber=@EmployeeNumber WHERE LabTechnicianID=@LabTechnicianID;
        MERGE dbo.UserAddress AS target USING (SELECT @UserID UserID) AS source ON target.UserID=source.UserID
        WHEN MATCHED THEN UPDATE SET AddressLine1=@AddressLine1,AddressLine2=NULLIF(@AddressLine2,N''),SuburbID=@SuburbID,UpdatedAt=SYSUTCDATETIME()
        WHEN NOT MATCHED THEN INSERT(UserID,AddressLine1,AddressLine2,SuburbID) VALUES(@UserID,@AddressLine1,NULLIF(@AddressLine2,N''),@SuburbID);
        COMMIT;
    END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH
END
GO
