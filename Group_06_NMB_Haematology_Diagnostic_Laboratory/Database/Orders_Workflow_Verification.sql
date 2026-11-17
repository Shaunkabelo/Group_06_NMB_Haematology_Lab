-- Isolated test rows, all rolled back; no supplier email.
SET NOCOUNT ON; SET XACT_ABORT ON;
DECLARE @Scenario int=1;
WHILE @Scenario<=5
BEGIN
 DECLARE @ExpectedFailure bit=0;
 BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @Supplier int=(SELECT TOP 1 SupplierID FROM dbo.Supplier WHERE IsActive=1);
 IF @Supplier IS NULL THROW 51000,'Active supplier required for tests.',1;
 DECLARE @Name nvarchar(150)=CONCAT('ORDER-TEST-',NEWID()),@A int,@B int,@Order int,@ItemA int,@ItemB int;
 INSERT dbo.Consumable(SupplierID,Name,ReorderLevel,QuantityOnHand,IsActive) VALUES(@Supplier,@Name,10,0,1);
 SET @A=CONVERT(int,SCOPE_IDENTITY()); SET @Name=CONCAT('ORDER-TEST-',NEWID());
 INSERT dbo.Consumable(SupplierID,Name,ReorderLevel,QuantityOnHand,IsActive) VALUES(@Supplier,@Name,10,0,1);
 SET @B=CONVERT(int,SCOPE_IDENTITY()); SET @Name=CONCAT('ORD-TEST-',NEWID());
 DECLARE @Created TABLE(ID int);
 DELETE @Created;
 INSERT @Created EXEC dbo.sp_StockOrder_Create @SupplierID=@Supplier,@OrderNumber=@Name;
 SELECT @Order=ID FROM @Created;
 EXEC dbo.sp_StockOrder_AddItem @StockOrderID=@Order,@ConsumableID=@A,@QuantityOrdered=5;
 IF @Scenario=5
 BEGIN
  SET @ExpectedFailure=1;
  EXEC dbo.sp_StockOrder_AddItem @StockOrderID=@Order,@ConsumableID=@B,@QuantityOrdered=0;
  THROW 51000,'Invalid item was accepted.',1;
 END;
 EXEC dbo.sp_StockOrder_AddItem @StockOrderID=@Order,@ConsumableID=@B,@QuantityOrdered=7;
 SELECT @ItemA=StockOrderItemID FROM dbo.StockOrderItem WHERE StockOrderID=@Order AND ConsumableID=@A;
 SELECT @ItemB=StockOrderItemID FROM dbo.StockOrderItem WHERE StockOrderID=@Order AND ConsumableID=@B;
 IF @Scenario=1
 BEGIN
  EXEC dbo.sp_StockOrder_ReceiveItem @StockOrderItemID=@ItemA;
  IF NOT EXISTS(SELECT 1 FROM dbo.StockOrder WHERE StockOrderID=@Order AND Status='Partially complete') THROW 51000,'Partial status failed.',1;
  EXEC dbo.sp_StockOrder_ReceiveItem @StockOrderID=@Order;
  IF NOT EXISTS(SELECT 1 FROM dbo.StockOrder WHERE StockOrderID=@Order AND Status='Complete' AND CompletionDate IS NOT NULL) THROW 51000,'Whole receipt failed.',1;
  IF (SELECT QuantityOnHand FROM dbo.Consumable WHERE ConsumableID=@A)<>5 OR (SELECT QuantityOnHand FROM dbo.Consumable WHERE ConsumableID=@B)<>7 THROW 51000,'Stock counted incorrectly.',1;
  SET @ExpectedFailure=1;
  EXEC dbo.sp_StockOrder_ReceiveItem @StockOrderID=@Order;
  THROW 51000,'Duplicate receipt accepted.',1;
 END;
 IF @Scenario=2
 BEGIN
  EXEC dbo.sp_StockOrder_ReceiveItem @StockOrderItemID=@ItemA;
  EXEC dbo.sp_StockOrder_CancelItem @StockOrderItemID=@ItemB,@CancelReason=N'No longer required';
  IF NOT EXISTS(SELECT 1 FROM dbo.StockOrder WHERE StockOrderID=@Order AND Status='Complete' AND CompletionDate IS NOT NULL) THROW 51000,'Completion date not captured.',1;
 END;
 IF @Scenario=3
 BEGIN
  EXEC dbo.sp_StockOrder_CancelItem @StockOrderItemID=@ItemA,@CancelReason=N'Cancelled';
  EXEC dbo.sp_StockOrder_CancelItem @StockOrderItemID=@ItemB,@CancelReason=N'Cancelled';
  IF NOT EXISTS(SELECT 1 FROM dbo.StockOrder WHERE StockOrderID=@Order AND Status='Cancelled' AND CancelledDate IS NOT NULL AND CancelReason IS NOT NULL) THROW 51000,'All cancelled status failed.',1;
 END;
 IF @Scenario=4
 BEGIN
  EXEC dbo.sp_StockOrder_ReceiveItem @StockOrderItemID=@ItemA;
  EXEC dbo.sp_StockOrder_Cancel @StockOrderID=@Order,@CancelReason=N'Cancel outstanding delivery';
  IF NOT EXISTS(SELECT 1 FROM dbo.StockOrderItem WHERE StockOrderItemID=@ItemA AND Status='Received') THROW 51000,'Received item overwritten.',1;
  IF NOT EXISTS(SELECT 1 FROM dbo.StockOrderItem WHERE StockOrderItemID=@ItemB AND Status='Cancelled' AND CancelledDate IS NOT NULL) THROW 51000,'Outstanding item not cancelled.',1;
  IF (SELECT QuantityOnHand FROM dbo.Consumable WHERE ConsumableID=@A)<>5 OR (SELECT QuantityOnHand FROM dbo.Consumable WHERE ConsumableID=@B)<>0 THROW 51000,'Cancellation changed stock.',1;
  SET @ExpectedFailure=1;
  EXEC dbo.sp_StockOrder_ReceiveItem @StockOrderItemID=@ItemB;
  THROW 51000,'Cancelled item received.',1;
 END;
 ROLLBACK;
 PRINT CONCAT('PASS scenario ',@Scenario);
 END TRY
 BEGIN CATCH
  DECLARE @Number int=ERROR_NUMBER();
  IF XACT_STATE()<>0 ROLLBACK;
  IF @ExpectedFailure=0 OR @Number<>50110 THROW;
  IF EXISTS(SELECT 1 FROM dbo.StockOrder WHERE OrderNumber=@Name) THROW 51000,'Failed operation left an order behind.',1;
  PRINT CONCAT('PASS scenario ',@Scenario,' including rejection and rollback');
 END CATCH;
 SET @Scenario+=1;
END;