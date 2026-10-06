SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
BEGIN TRANSACTION;
DECLARE @Category int=(SELECT TOP 1 TestCategoryID FROM dbo.TestCategory WHERE IsActive=1);
DECLARE @Sample int=(SELECT TOP 1 SampleTypeID FROM dbo.SampleType WHERE IsActive=1);
DECLARE @Supplier int=(SELECT TOP 1 SupplierID FROM dbo.Supplier WHERE IsActive=1);
IF @Category IS NULL OR @Sample IS NULL OR @Supplier IS NULL THROW 51000,'Active catalogue prerequisites are needed.',1;
DECLARE @Name nvarchar(150)=CONCAT('Regression-',NEWID());
INSERT dbo.Consumable(SupplierID,Name,ReorderLevel,QuantityOnHand,IsActive) VALUES(@Supplier,@Name,10,0,1);
DECLARE @A int=CONVERT(int,SCOPE_IDENTITY());
SET @Name=CONCAT('Regression-',NEWID());
INSERT dbo.Consumable(SupplierID,Name,ReorderLevel,QuantityOnHand,IsActive) VALUES(@Supplier,@Name,10,0,1);
DECLARE @B int=CONVERT(int,SCOPE_IDENTITY());
SET @Name=CONCAT('Regression-',NEWID());
DECLARE @Created TABLE(ID int);
INSERT @Created EXEC dbo.sp_TestType_Create @TestCategoryID=@Category,@TestName=@Name,@SampleTypeID=@Sample,
    @UnitOfMeasurement=N'g/L',@NormalRangeMin=1,@NormalRangeMax=10,@TurnaroundTimeMinutes=15,@ConsumableID=@A,@QuantityRequired=100;
DECLARE @Test int=(SELECT ID FROM @Created);
DECLARE @Link int=(SELECT TestTypeConsumableID FROM dbo.TestTypeConsumable WHERE TestTypeID=@Test);
EXEC dbo.sp_TestType_Update @TestTypeID=@Test,@TestCategoryID=@Category,@TestName=@Name,@SampleTypeID=@Sample,
    @UnitOfMeasurement=N'g/L',@NormalRangeMin=2,@NormalRangeMax=12,@TurnaroundTimeMinutes=20;
EXEC dbo.sp_TestType_Update @TestTypeID=@Test,@TestCategoryID=@Category,@TestName=@Name,@SampleTypeID=@Sample,
    @UnitOfMeasurement=N'g/L',@NormalRangeMin=2,@NormalRangeMax=12,@TurnaroundTimeMinutes=20,
    @TestTypeConsumableID=@Link,@ConsumableID=@B,@QuantityRequired=150;
EXEC dbo.sp_TestTypeConsumable_Update @TestTypeConsumableID=@Link,@TestTypeID=@Test,@ConsumableID=@A,@QuantityRequired=200;
EXEC dbo.sp_TestTypeConsumable_Update @TestTypeConsumableID=@Link,@TestTypeID=@Test,@ConsumableID=@A,@QuantityRequired=300;
EXEC dbo.sp_TestTypeConsumable_Create @TestTypeID=@Test,@ConsumableID=@B,@QuantityRequired=50;
DECLARE @Second int=(SELECT TestTypeConsumableID FROM dbo.TestTypeConsumable WHERE TestTypeID=@Test AND ConsumableID=@B);
EXEC dbo.sp_TestTypeConsumable_Delete @TestTypeConsumableID=@Second,@TestTypeID=@Test;
EXEC dbo.sp_TestType_Delete @TestTypeID=@Test;
IF EXISTS(SELECT 1 FROM dbo.TestType WHERE TestTypeID=@Test AND IsActive<>0) THROW 51000,'Deactivation failed.',1;
EXEC dbo.sp_TestType_Reactivate @TestTypeID=@Test;
IF NOT EXISTS(SELECT 1 FROM dbo.TestType WHERE TestTypeID=@Test AND IsActive=1) THROW 51000,'Reactivation failed.',1;
IF NOT EXISTS(SELECT 1 FROM dbo.TestTypeConsumable WHERE TestTypeConsumableID=@Link AND QuantityRequired=300 AND ConsumableID=@A)
    THROW 51000,'Requirement was not updated.',1;
IF EXISTS(SELECT 1 FROM dbo.Consumable WHERE ConsumableID IN (@A,@B) AND QuantityOnHand<>0)
    THROW 51000,'Catalogue operation changed physical stock.',1;
IF EXISTS(SELECT 1 FROM dbo.ConsumableTransaction WHERE ConsumableID IN (@A,@B))
    THROW 51000,'Catalogue operation wrote a stock transaction.',1;
ROLLBACK;
PRINT 'PASS: create, edit, change consumable, add/remove requirement, deactivate/reactivate all preserve zero stock and write no stock audit entries. Test rows rolled back.';
END TRY
BEGIN CATCH
IF XACT_STATE()<>0 ROLLBACK;
THROW;
END CATCH;