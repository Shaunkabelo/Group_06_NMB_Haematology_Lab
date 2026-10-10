-- Execute the whole script. Preview first; then set @Apply=1 to install.
-- Deployment does not rewrite historical orders or stock.
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
DECLARE @ExpectedDatabase sysname=N'GRP-04-06', @Apply bit=0;
IF DB_NAME()<>@ExpectedDatabase OR DB_ID()<=4 THROW 51000,'Select the application database and set @ExpectedDatabase.',1;
IF @@TRANCOUNT<>0 THROW 51000,'Run without an existing transaction.',1;
IF OBJECT_ID('dbo.StockOrder','U') IS NULL OR OBJECT_ID('dbo.StockOrderItem','U') IS NULL OR OBJECT_ID('dbo.Consumable','U') IS NULL
 THROW 51000,'Existing order and stock tables are required.',1;
BEGIN TRY
BEGIN TRANSACTION;
EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE dbo.sp_StockOrder_Create @SupplierID int,@OrderNumber nvarchar(50)
AS BEGIN
 SET NOCOUNT ON;
 IF NULLIF(LTRIM(RTRIM(@OrderNumber)),'''') IS NULL THROW 50110,''An order number is required.'',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.Supplier WHERE SupplierID=@SupplierID AND IsActive=1) THROW 50110,''Choose an active supplier.'',1;
 INSERT dbo.StockOrder(SupplierID,OrderNumber,OrderDate,Status) VALUES(@SupplierID,@OrderNumber,SYSUTCDATETIME(),''Ordered'');
 SELECT CONVERT(int,SCOPE_IDENTITY());
END;';
EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE dbo.sp_StockOrder_AddItem @StockOrderID int,@ConsumableID int,@QuantityOrdered int
AS BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @Supplier int;
 SELECT @Supplier=SupplierID FROM dbo.StockOrder WITH(UPDLOCK,HOLDLOCK) WHERE StockOrderID=@StockOrderID AND Status=''Ordered'';
 IF @Supplier IS NULL THROW 50110,''Only an open order can accept items.'',1;
 IF @QuantityOrdered IS NULL OR @QuantityOrdered<=0 THROW 50110,''Quantity must be a positive whole number.'',1;
 IF NOT EXISTS(SELECT 1 FROM dbo.Consumable WITH(HOLDLOCK) WHERE ConsumableID=@ConsumableID AND SupplierID=@Supplier AND IsActive=1)
 THROW 50110,''Consumable must be active and belong to the order supplier.'',1;
 IF EXISTS(SELECT 1 FROM dbo.StockOrderItem WHERE StockOrderID=@StockOrderID AND ConsumableID=@ConsumableID) THROW 50110,''Consumable is already on this order.'',1;
 INSERT dbo.StockOrderItem(StockOrderID,ConsumableID,QuantityOrdered,Status) VALUES(@StockOrderID,@ConsumableID,@QuantityOrdered,''Ordered'');
 COMMIT;
 END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; THROW; END CATCH;
END;';
EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE dbo.sp_StockOrder_ReceiveItem @StockOrderItemID int=NULL,@StockOrderID int=NULL
AS BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 IF (@StockOrderItemID IS NULL AND @StockOrderID IS NULL) OR (@StockOrderItemID IS NOT NULL AND @StockOrderID IS NOT NULL)
 THROW 50110,''Specify one item or one whole order.'',1;
 IF @StockOrderItemID IS NOT NULL SELECT @StockOrderID=StockOrderID FROM dbo.StockOrderItem WHERE StockOrderItemID=@StockOrderItemID;
 BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @Status nvarchar(30),@Now datetime2=SYSUTCDATETIME();
 SELECT @Status=Status FROM dbo.StockOrder WITH(UPDLOCK,HOLDLOCK) WHERE StockOrderID=@StockOrderID;
 IF @Status IS NULL OR @Status NOT IN (''Ordered'',''Partially complete'') THROW 50110,''This order is no longer available for receipt. Refresh the page.'',1;
 DECLARE @Items TABLE(ID int PRIMARY KEY,ConsumableID int,Quantity int);
 INSERT @Items SELECT StockOrderItemID,ConsumableID,QuantityOrdered FROM dbo.StockOrderItem WITH(UPDLOCK,HOLDLOCK)
 WHERE StockOrderID=@StockOrderID AND Status=''Ordered'' AND (@StockOrderItemID IS NULL OR StockOrderItemID=@StockOrderItemID);
 IF NOT EXISTS(SELECT 1 FROM @Items) THROW 50110,''No outstanding items can be received. Refresh the page.'',1;
 IF EXISTS(SELECT 1 FROM @Items WHERE Quantity<=0) THROW 50110,''Invalid ordered quantity.'',1;
 IF EXISTS(SELECT 1 FROM dbo.Consumable c WITH(UPDLOCK,HOLDLOCK)
 JOIN (SELECT ConsumableID,SUM(CONVERT(bigint,Quantity)) Qty FROM @Items GROUP BY ConsumableID) i ON i.ConsumableID=c.ConsumableID
 WHERE CONVERT(bigint,c.QuantityOnHand)+i.Qty>2147483647) THROW 50110,''Receipt would exceed the stock quantity limit.'',1;
 UPDATE c SET QuantityOnHand=CONVERT(int,CONVERT(bigint,c.QuantityOnHand)+i.Qty)
 FROM dbo.Consumable c JOIN (SELECT ConsumableID,SUM(CONVERT(bigint,Quantity)) Qty FROM @Items GROUP BY ConsumableID) i ON i.ConsumableID=c.ConsumableID;
 UPDATE s SET Status=''Received'',DateReceived=@Now FROM dbo.StockOrderItem s JOIN @Items i ON i.ID=s.StockOrderItemID;
 UPDATE dbo.StockOrder SET
 Status=CASE WHEN EXISTS(SELECT 1 FROM dbo.StockOrderItem WHERE StockOrderID=@StockOrderID AND Status=''Ordered'') THEN ''Partially complete'' ELSE ''Complete'' END,
 CompletionDate=CASE WHEN EXISTS(SELECT 1 FROM dbo.StockOrderItem WHERE StockOrderID=@StockOrderID AND Status=''Ordered'') THEN NULL ELSE @Now END
 WHERE StockOrderID=@StockOrderID;
 COMMIT;
 END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; THROW; END CATCH;
END;';
EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE dbo.sp_StockOrder_CancelItem @StockOrderItemID int,@CancelReason nvarchar(max)
AS BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 SET @CancelReason=LTRIM(RTRIM(@CancelReason));
 IF @CancelReason IS NULL OR LEN(@CancelReason)=0 OR LEN(@CancelReason)>255 THROW 50110,''Enter a cancellation reason of 1-255 characters.'',1;
 DECLARE @Order int=(SELECT StockOrderID FROM dbo.StockOrderItem WHERE StockOrderItemID=@StockOrderItemID);
 BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @Status nvarchar(30),@Now datetime2=SYSUTCDATETIME();
 SELECT @Status=Status FROM dbo.StockOrder WITH(UPDLOCK,HOLDLOCK) WHERE StockOrderID=@Order;
 IF @Status IS NULL OR @Status NOT IN (''Ordered'',''Partially complete'') THROW 50110,''The order is already closed. Refresh the page.'',1;
 UPDATE dbo.StockOrderItem SET Status=''Cancelled'',CancelReason=@CancelReason,CancelledDate=@Now WHERE StockOrderItemID=@StockOrderItemID AND Status=''Ordered'';
 IF @@ROWCOUNT<>1 THROW 50110,''Only outstanding items can be cancelled. Refresh the page.'',1;
 DECLARE @Open bit=CASE WHEN EXISTS(SELECT 1 FROM dbo.StockOrderItem WHERE StockOrderID=@Order AND Status=''Ordered'') THEN 1 ELSE 0 END;
 DECLARE @Received bit=CASE WHEN EXISTS(SELECT 1 FROM dbo.StockOrderItem WHERE StockOrderID=@Order AND Status=''Received'') THEN 1 ELSE 0 END;
 UPDATE dbo.StockOrder SET
 Status=CASE WHEN @Open=0 AND @Received=0 THEN ''Cancelled'' WHEN @Open=0 THEN ''Complete'' WHEN @Received=1 THEN ''Partially complete'' ELSE ''Ordered'' END,
 CompletionDate=CASE WHEN @Open=0 AND @Received=1 THEN @Now ELSE NULL END,
 CancelledDate=CASE WHEN @Open=0 AND @Received=0 THEN @Now ELSE CancelledDate END,
 CancelReason=CASE WHEN @Open=0 AND @Received=0 THEN @CancelReason ELSE CancelReason END WHERE StockOrderID=@Order;
 COMMIT;
 END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; THROW; END CATCH;
END;';
EXEC sys.sp_executesql N'CREATE OR ALTER PROCEDURE dbo.sp_StockOrder_Cancel @StockOrderID int,@CancelReason nvarchar(max)
AS BEGIN
 SET NOCOUNT ON; SET XACT_ABORT ON;
 SET @CancelReason=LTRIM(RTRIM(@CancelReason));
 IF @CancelReason IS NULL OR LEN(@CancelReason)=0 OR LEN(@CancelReason)>255 THROW 50110,''Enter a cancellation reason of 1-255 characters.'',1;
 BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @Status nvarchar(30),@Now datetime2=SYSUTCDATETIME();
 SELECT @Status=Status FROM dbo.StockOrder WITH(UPDLOCK,HOLDLOCK) WHERE StockOrderID=@StockOrderID;
 IF @Status IS NULL OR @Status NOT IN (''Ordered'',''Partially complete'') THROW 50110,''Only an open order can be cancelled. Refresh the page.'',1;
 UPDATE dbo.StockOrderItem SET Status=''Cancelled'',CancelReason=@CancelReason,CancelledDate=@Now WHERE StockOrderID=@StockOrderID AND Status=''Ordered'';
 UPDATE dbo.StockOrder SET Status=''Cancelled'',CancelReason=@CancelReason,CancelledDate=@Now,CompletionDate=NULL WHERE StockOrderID=@StockOrderID;
 COMMIT;
 END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; THROW; END CATCH;
END;';
IF @Apply=1 BEGIN COMMIT; PRINT 'Order workflow installed.'; END
ELSE BEGIN ROLLBACK; PRINT 'Preview passed; rolled back. Set @Apply=1 to install.'; END;
END TRY BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK; THROW; END CATCH;
