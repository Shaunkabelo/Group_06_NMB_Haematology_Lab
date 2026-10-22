/*
    Optional, additive query-performance indexes.

    Run once against the application database after taking a backup.  Each
    statement is idempotent: an index is created only when its name is absent.
    They support dashboard totals, staff lookups, report date filtering,
    stock-order lists, and address dropdowns. No application data is changed.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @ExpectedDatabase sysname = N'GRP-04-06';
IF DB_NAME() <> @ExpectedDatabase OR DB_ID() <= 4
    THROW 51000, 'Select the application database before running this script.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.[User]') AND name = N'IX_User_Role_IsActive')
    CREATE INDEX IX_User_Role_IsActive ON dbo.[User] ([Role], IsActive) INCLUDE (UserID, Name, Surname, Email);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.LabTest') AND name = N'IX_LabTest_CompletionDateTime')
    CREATE INDEX IX_LabTest_CompletionDateTime ON dbo.LabTest (CompletionDateTime) INCLUDE (TestRequestTestTypeID);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.TestRequestTestType') AND name = N'IX_TestRequestTestType_TestType')
    CREATE INDEX IX_TestRequestTestType_TestType ON dbo.TestRequestTestType (TestTypeID) INCLUDE (TestRequestID);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.StockOrder') AND name = N'IX_StockOrder_Status_OrderDate')
    CREATE INDEX IX_StockOrder_Status_OrderDate ON dbo.StockOrder (Status, OrderDate DESC) INCLUDE (SupplierID);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Consumable') AND name = N'IX_Consumable_Active_Stock')
    CREATE INDEX IX_Consumable_Active_Stock ON dbo.Consumable (IsActive, QuantityOnHand, ReorderLevel) INCLUDE (SupplierID, Name);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.TownCity') AND name = N'IX_TownCity_Province_Active')
    CREATE INDEX IX_TownCity_Province_Active ON dbo.TownCity (ProvinceID, IsActive, TownCityName);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.Suburb') AND name = N'IX_Suburb_TownCity_Active')
    CREATE INDEX IX_Suburb_TownCity_Active ON dbo.Suburb (TownCityID, IsActive, SuburbName) INCLUDE (AreaCode);

PRINT 'Performance indexes checked and created where missing.';
