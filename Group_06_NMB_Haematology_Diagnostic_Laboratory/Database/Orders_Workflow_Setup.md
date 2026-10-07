# Orders workflow deployment — 7 October 2026

## Hosted database

1. Open Orders_Workflow.sql in SSMS against the existing application database.
2. Set @ExpectedDatabase to its exact name (default GRP-04-06).
3. Run the whole file with @Apply=0. Confirm the preview passed.
4. Set @Apply=1 and rerun to install.
5. Publish the updated application after the database update.

This updates five EXISTING procedures, preserving names and existing callers:
sp_StockOrder_Create, sp_StockOrder_AddItem, sp_StockOrder_ReceiveItem,
sp_StockOrder_CancelItem and sp_StockOrder_Cancel.
ReceiveItem now also accepts @StockOrderID, exclusively instead of an item ID,
to receive every outstanding item in one transaction.

Deployment does not adjust historical stock or repair orders already inconsistent
from older procedures. Review those separately. Do not reapply older order scripts
after this migration.

## Behaviour

- Creating an order saves its header and all items through the same Dapper
  connection and transaction. Any item failure rolls back the entire order.
- Database checks enforce active suppliers/consumables, supplier ownership,
  positive quantities and no duplicate consumable lines.
- Order changes acquire a transaction lock on the order header first.
- Item receipt increments stock once and records DateReceived.
- Whole-order receipt includes only Ordered items; Received and Cancelled items
  are skipped. Stock overflow rejects the entire receipt.
- A duplicate/stale receipt or cancellation returns a clear refresh message.
- Item cancellation records its reason/date without changing stock. The order
  becomes Cancelled if all items are cancelled, or closes with CompletionDate
  when received items remain and no items are outstanding.
- Whole-order cancellation cancels outstanding items and the header, preserving
  received items and their stock.
- Screens display Received/Partially Received. Stored values remain
  Complete/Partially complete for compatibility.

## Supplier emails

Existing Email configuration (same SMTP service as account emails) is reused.
The supplier's SupplierEmail receives:
- New order notification after successful order creation.
- Item cancellation notification with item, quantity and reason.
- Whole-order cancellation notification with the reason and current item states.

Messages include order number/date, consumable names, quantities and statuses.
After automatic sending, the Orders page displays a separate notification:
success identifies the supplier email and order number; failure warns that the order
change is saved but email could not be confirmed. The manual resend action/button
has been removed. Notifications can be dismissed and do not require another click to send.

Email is sent after the database commit. SMTP acceptance does not prove inbox delivery
or that the supplier read it. There is no durable automatic retry queue. On failure,
the notification directs the manager to contact the supplier and check email configuration;
do not repeat the order operation to resend email.
## Verification

- Local database migration installed; hosted database not changed in this session.
- Orders_Workflow_Verification.sql passes rollback-only scenarios for partial/full
  receipt, duplicate rejection, closing after final-item cancellation, all-items
  cancellation, preservation of received items and atomic failure of an invalid item.
- Two concurrent receipt connections were tested using isolated test data:
  the second request waited, then failed as stale; stock increased exactly once.
  The isolated test data was removed.
- 83 application checks passed, including notification success/failure,
  no email after failed creation, cancellation reasons, whole-order receipt routing
  and automatic success/failure notifications.
- No real SMTP message was sent during automated testing.
- Existing five data-layer nullability warnings remain.