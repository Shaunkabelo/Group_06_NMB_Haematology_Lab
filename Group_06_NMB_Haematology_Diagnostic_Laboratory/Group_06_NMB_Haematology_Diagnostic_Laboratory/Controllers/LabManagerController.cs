using Group_06_NMB_Haematology_Diagnostic_Laboratory.Services;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Helpers;
using Group_06_NMB_Haematology_Diagnostic_Laboratory.Models;




namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers
{
    [ServiceFilter(typeof(Group_06_NMB_Haematology_Diagnostic_Laboratory.Validation.StaffValidationFilter))]
    [Authorize(Roles = "LabManager,Admin")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class LabManagerController : Controller
    {
        private string CurrentReportGenerator()
        {
            var name = User.Identity?.Name ?? "Lab Manager";
            var role = User.IsInRole("LabManager") ? "Lab Manager"
                : User.IsInRole("Admin") ? "Administrator"
                : "Lab Manager";
            return $"{name} ({role})";
        }

        private async Task SendWelcomeEmail(User user, string temporaryPassword)
        {
            try
            {
                await _emailSender.SendTemporaryPassword(user.Email, user.Name, temporaryPassword);
                TempData["Success"] = "Account created. A temporary password has been emailed to the user.";
            }
            catch (Exception exception)
            {
                _logger.LogError("Account created but welcome email failed ({FailureType}).", exception.GetType().Name);
                TempData["Error"] = "The account was created, but its welcome email could not be sent. Do not create it again. The user can use Forgot password once email delivery is restored.";
            }
        }
        private readonly ILabManagerRepository _repository;

        private readonly IAccountEmailSender _emailSender;
        private readonly ILogger<LabManagerController> _logger;
        public LabManagerController(ILabManagerRepository repository, IAccountEmailSender emailSender, ILogger<LabManagerController> logger)
        {
            _repository = repository;
            _emailSender = emailSender;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> AddressTownCities(int provinceId)
        {
            if (provinceId <= 0) return Json(Array.Empty<object>());
            return Json(await _repository.GetTownCities(provinceId));
        }

        [HttpGet]
        public async Task<IActionResult> AddressSuburbs(int townCityId)
        {
            if (townCityId <= 0) return Json(Array.Empty<object>());
            return Json(await _repository.GetSuburbs(townCityId));
        }
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var model = new DashboardViewModel
            {
                TestTypeCount = (await _repository.GetTestTypes("Active")).Count(),
                CategoryCount = (await _repository.GetTestCategories("Active")).Count(),
                DoctorCount = (await _repository.GetDoctors("Active")).Count(),
                TechnicianCount = (await _repository.GetTechnicians("Active")).Count(),
                ManagerCount = (await _repository.GetManagers("Active")).Count(),
                ConsumableCount = (await _repository.GetConsumables("Active")).Count(),
                LowStockConsumables = (await _repository.GetLowStockConsumables()).ToList(),
                RecentOrders = (await _repository.GetStockOrders("All"))
                    .OrderByDescending(order => order.OrderDate)
                    .Take(5)
                    .ToList()
            };

            return View(model);
        }


        [HttpGet]
        public async Task<IActionResult> Orders(string filter = "All", string search = "")
        {
            var orders = await _repository.GetStockOrders(filter, search);

            // The page deliberately has the stock context beside the orders so a
            // manager can make a purchasing decision without changing screens.
            ViewBag.Consumables = await _repository.GetConsumables("Active");
            ViewBag.LowStockConsumables = await _repository.GetLowStockConsumables();
            ViewBag.Suppliers = await _repository.GetSuppliers("Active");
            ViewBag.Filter = filter;
            ViewBag.Search = search;

            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOrder(
            int supplierId,
            string? orderNumber,
            [FromForm] int[] selectedConsumables,
            IFormCollection form)
        {
            var selectedIds = selectedConsumables.Distinct().ToList();
            if (!ModelState.IsValid || supplierId <= 0 || selectedIds.Count == 0)
            {
                TempData["ErrorMessage"] = "Select a supplier and at least one consumable.";
                return RedirectToAction(nameof(Orders));
            }

            var supplier = await _repository.GetSupplierById(supplierId);
            var consumables = (await _repository.GetConsumables("Active"))
                .ToDictionary(c => c.ConsumableId);

            if (supplier == null || !supplier.IsActive || selectedIds.Any(id => !consumables.ContainsKey(id) || consumables[id].SupplierId != supplierId))
            {
                TempData["ErrorMessage"] = "The supplier or one of the selected consumables is no longer available.";
                return RedirectToAction(nameof(Orders));
            }

            var items = new List<(int ConsumableId, int Quantity)>();
            foreach (var consumableId in selectedIds)
            {
                if (!int.TryParse(form[$"quantity_{consumableId}"], out var quantity) || quantity <= 0)
                {
                    TempData["ErrorMessage"] = "Each selected consumable needs a quantity greater than zero.";
                    return RedirectToAction(nameof(Orders));
                }

                items.Add((consumableId, quantity));
            }

            var safeOrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8]}";

            int stockOrderId;
            try
            {
                stockOrderId = await _repository.CreateStockOrderWithItems(new StockOrder
                { SupplierId = supplierId, OrderNumber = safeOrderNumber, OrderDate = DateTime.UtcNow }, items);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 50110 or 2601 or 2627 or 1205)
            {
                TempData["ErrorMessage"] = ex.Number == 50110 ? ex.Message : "The order could not be saved. Refresh and try again.";
                return RedirectToAction(nameof(Orders));
            }
            TempData["SuccessMessage"] = $"Order {safeOrderNumber} was created.";
            await NotifySupplier(stockOrderId, "A new consumable stock order has been placed.");
            return RedirectToAction(nameof(Orders));
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderDetails(int id)
        {
            var order = await _repository.GetStockOrderById(id);
            if (order == null)
            {
                return NotFound();
            }

            // Return only data the browser needs; EF navigation properties would
            // otherwise create circular JSON graphs.
            return Json(new
            {
                order.StockOrderId,
                order.OrderNumber,
                order.OrderDate,
                order.Status,
                order.CompletionDate,
                order.CancelledDate,
                order.CancelReason,
                Supplier = new
                {
                    order.Supplier?.SupplierId,
                    order.Supplier?.SupplierName,
                    order.Supplier?.ContactPerson,
                    order.Supplier?.SupplierEmail
                },
                StockOrderItem = order.StockOrderItem.Select(item => new
                {
                    item.StockOrderItemId,
                    item.ConsumableId,
                    item.QuantityOrdered,
                    item.Status,
                    item.DateReceived,
                    item.CancelReason,
                    item.CancelledDate,
                    Consumable = new { item.Consumable?.Name }
                })
            });
        }

        private async Task NotifySupplier(int id, string notification)
        {
            try
            {
                var order = await _repository.GetStockOrderById(id)
                    ?? throw new InvalidOperationException("Order unavailable after save.");
                await _emailSender.SendSupplierNotification(order, notification);
                TempData["SupplierEmailStatus"] = "success";
                TempData["SupplierEmailNotification"] = $"Supplier email sent automatically to {order.Supplier?.SupplierEmail} for order {order.OrderNumber}. Accepted by the mail server; this does not confirm it has been read.";
            }
            catch (Exception ex)
            {
                _logger.LogError("Order {OrderId} saved but supplier notification failed ({FailureType}).", id, ex.GetType().Name);
                TempData["SupplierEmailStatus"] = "warning";
                TempData["SupplierEmailNotification"] = "The order change is saved, but the automatic supplier email could not be confirmed. Contact the supplier directly and check the email configuration. Do not create or cancel the order again to resend an email.";
            }
        }

        private async Task<IActionResult> ChangeOrder(Func<Task> change, int id, string success, string? notification = null)
        {
            if (!ModelState.IsValid || id <= 0)
            { TempData["ErrorMessage"] = "Choose a valid order."; return RedirectToAction(nameof(Orders)); }
            try { await change(); }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is 50110 or 1205)
            {
                TempData["ErrorMessage"] = ex.Number == 50110 ? ex.Message : "The order was being changed. Refresh and try again.";
                return RedirectToAction(nameof(Orders));
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogError(ex, "Order {OrderId} workflow operation failed.", id);
                TempData["ErrorMessage"] = "The order could not be updated because the database workflow is not installed or rejected the request. Run Database/Orders_Workflow.sql, then try again.";
                return RedirectToAction(nameof(Orders));
            }
            TempData["SuccessMessage"] = success;
            if (notification != null) await NotifySupplier(id, notification);
            return RedirectToAction(nameof(Orders));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveOrderItem(int stockOrderItemId, int stockOrderId)
        {
            var order = await _repository.GetStockOrderById(stockOrderId);
            if (order == null || !order.StockOrderItem.Any(x => x.StockOrderItemId == stockOrderItemId)) return NotFound();
            return await ChangeOrder(() => _repository.ReceiveStockOrderItem(stockOrderItemId), stockOrderId,
                "Order item received and stock updated.");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveOrder(int stockOrderId)
        {
            var order = await _repository.GetStockOrderById(stockOrderId);
            if (order == null) return NotFound();
            if (!order.StockOrderItem.Any(item => string.Equals(item.Status, "Ordered", StringComparison.OrdinalIgnoreCase)))
            {
                TempData["ErrorMessage"] = "There are no remaining items to receive on this order.";
                return RedirectToAction(nameof(Orders));
            }

            return await ChangeOrder(() => _repository.ReceiveStockOrder(stockOrderId), stockOrderId,
                "All remaining items received and stock updated.");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrderItem(int stockOrderItemId, string? cancelReason, int stockOrderId)
        {
            if (string.IsNullOrWhiteSpace(cancelReason) || cancelReason.Trim().Length > 255)
            { TempData["ErrorMessage"] = "Enter a cancellation reason of 1–255 characters."; return RedirectToAction(nameof(Orders)); }
            var order = await _repository.GetStockOrderById(stockOrderId);
            var item = order?.StockOrderItem.FirstOrDefault(x => x.StockOrderItemId == stockOrderItemId);
            if (item == null) return NotFound();
            return await ChangeOrder(() => _repository.CancelStockOrderItem(stockOrderItemId, cancelReason.Trim()), stockOrderId,
                "Order item cancelled.", $"Cancellation: {item.Consumable?.Name}, quantity {item.QuantityOrdered}. Reason: {cancelReason.Trim()}");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int stockOrderId, string? cancelReason)
        {
            if (string.IsNullOrWhiteSpace(cancelReason) || cancelReason.Trim().Length > 255)
            { TempData["ErrorMessage"] = "Enter a cancellation reason of 1–255 characters."; return RedirectToAction(nameof(Orders)); }
            var order = await _repository.GetStockOrderById(stockOrderId);
            if (order == null) return NotFound();
            if (!order.StockOrderItem.Any(item => string.Equals(item.Status, "Ordered", StringComparison.OrdinalIgnoreCase)))
            {
                TempData["ErrorMessage"] = "There are no remaining items to cancel on this order.";
                return RedirectToAction(nameof(Orders));
            }
            return await ChangeOrder(() => _repository.CancelStockOrder(stockOrderId, cancelReason.Trim()), stockOrderId,
                "Order cancelled. Received items were preserved; outstanding items were cancelled.",
                $"The outstanding items on this order have been cancelled. Reason: {cancelReason.Trim()}");
        }

        // ==================== SAMPLE TYPES ====================

        // ==================== DISPLAY SAMPLE TYPES ====================
        [HttpGet]
        public async Task<IActionResult> SampleTypes(
            string filter = "Active",
            string search = "")
        {
            var sampleTypes = await _repository.GetSampleTypes(filter, search);

            ViewBag.Filter = filter;
            ViewBag.Search = search;

            return View(sampleTypes);
        }


        // ==================== CREATE SAMPLE TYPE ====================
        [HttpPost]
        public async Task<IActionResult> CreateSampleType(SampleType sampleType)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(SampleTypes));
            }

            await _repository.CreateSampleType(sampleType);

            return RedirectToAction(nameof(SampleTypes));
        }


        // ==================== UPDATE SAMPLE TYPE ====================
        [HttpPost]
        public async Task<IActionResult> EditSampleType(SampleType sampleType)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(SampleTypes));
            }

            await _repository.UpdateSampleType(sampleType);

            return RedirectToAction(nameof(SampleTypes));
        }


        // ==================== DELETE SAMPLE TYPE ====================
        [HttpPost]
        public async Task<IActionResult> DeleteSampleType(
            int id,
            string filter = "Active",
            string search = "")
        {
            await _repository.DeleteSampleType(id);

            return RedirectToAction(
                nameof(SampleTypes),
                new
                {
                    filter,
                    search
                });
        }


        // ==================== REACTIVATE SAMPLE TYPE ====================
        [HttpPost]
        public async Task<IActionResult> ReactivateSampleType(
            int id,
            string filter = "Inactive",
            string search = "")
        {
            await _repository.ReactivateSampleType(id);

            return RedirectToAction(
                nameof(SampleTypes),
                new
                {
                    filter,
                    search
                });
        }



        // ---------==================== TEST CATEGORIES ====================----------------

        // ==================== DISPLAY TEST CATEGORIES ====================
        [HttpGet]
        public async Task<IActionResult> TestCategories(
            string filter = "Active",
            string search = "")
        {
            var categories = await _repository.GetTestCategories(filter, search);

            ViewBag.Filter = filter;
            ViewBag.Search = search;

            return View(categories);
        }


        // ==================== CREATE TEST CATEGORY ====================
        [HttpPost]
        public async Task<IActionResult> CreateTestCategory(TestCategory category)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(TestCategories));
            }

            await _repository.CreateTestCategory(category);

            return RedirectToAction(nameof(TestCategories));
        }


        // ==================== UPDATE TEST CATEGORY ====================
        [HttpPost]
        public async Task<IActionResult> EditTestCategory(TestCategory category)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(TestCategories));
            }

            await _repository.UpdateTestCategory(category);

            return RedirectToAction(nameof(TestCategories));
        }


        // ==================== DELETE TEST CATEGORY ====================
        [HttpPost]
        public async Task<IActionResult> DeleteTestCategory(
            int id,
            string filter = "Active",
            string search = "")
        {
            await _repository.DeleteTestCategory(id);

            return RedirectToAction(
                nameof(TestCategories),
                new
                {
                    filter,
                    search
                });
        }


        // ==================== REACTIVATE TEST CATEGORY ====================
        [HttpPost]
        public async Task<IActionResult> ReactivateTestCategory(
            int id,
            string filter = "Inactive",
            string search = "")
        {
            await _repository.ReactivateTestCategory(id);

            return RedirectToAction(
                nameof(TestCategories),
                new
                {
                    filter,
                    search
                });
        }

        // ==================== TEST TYPES ========================================================================
        // =========================================================================================================

        // ==================== DISPLAY TEST TYPES ====================

        [HttpGet]
        public async Task<IActionResult> TestTypes(
    string filter = "Active",
    string search = "", int? categoryId = null)
        {
            var testTypes = await _repository.GetTestTypes(filter, search);
            if (categoryId.HasValue) testTypes = testTypes.Where(t => t.TestCategoryId == categoryId.Value);
            ViewBag.CategoryId = categoryId;
            ViewBag.AssignmentTechnicians = (await _repository.GetTechnicians("All")).ToList();
            ViewBag.TestTypeAssignments = (await _repository.GetTestTypeAssignments()).ToList();

            var categories = await _repository.GetTestCategories();

            var sampleTypes = await _repository.GetSampleTypes();

            var consumables = await _repository.GetConsumables("Active");

            ViewBag.Filter = filter;
            ViewBag.Search = search;

            ViewBag.Categories = categories;
            ViewBag.SampleTypes = sampleTypes;
            ViewBag.Consumables = consumables;

            return View(testTypes);
        }


        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTestTypeTechnicians(int testTypeId, int[] technicianIds,
            string? expectedTechnicianIds, string filter = "Active", string search = "", int? categoryId = null)
        {
            technicianIds ??= Array.Empty<int>();
            if (!ModelState.IsValid || testTypeId <= 0 || technicianIds.Any(id => id <= 0))
            {
                TempData["Error"] = "Choose valid technicians.";
                return RedirectToAction(nameof(TestTypes), new { filter, search, categoryId });
            }
            try
            {
                await _repository.ReplaceTestTypeTechnicians(testTypeId, technicianIds, expectedTechnicianIds ?? "");
                TempData["Success"] = "Technician assignments saved.";
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is >= 50100 and <= 50103)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(TestTypes), new { filter, search, categoryId });
        }
        // ==================== CREATE TEST TYPE ====================

        [HttpPost]
        public async Task<IActionResult> CreateTestType(
            [Bind("TestCategoryId,TestName,SampleTypeId,UnitOfMeasurement,NormalRangeMin,NormalRangeMax,TurnaroundTimeMinutes")] TestType testType,
            List<TestTypeConsumable> requirements)
        {
            var validRequirements = requirements?.Where(r => r.ConsumableId > 0 && r.QuantityRequired > 0).ToList()
                ?? new List<TestTypeConsumable>();
            if (!validRequirements.Any())
            {
                TempData["Error"] = "Select a consumable and enter a quantity greater than zero.";
                return RedirectToAction(
                    nameof(TestTypes),
                    new { filter = "Active" });
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(value => value.Errors)
                    .Select(error => error.ErrorMessage)
                    .Where(message => !string.IsNullOrWhiteSpace(message));

                TempData["Error"] = errors.Any()
                    ? string.Join(" ", errors)
                    : "Enter valid test type details.";
                return RedirectToAction(nameof(TestTypes), new { filter = "Active" });
            }

            foreach (var requirement in validRequirements)
            {
                var consumable = await _repository.GetConsumableById(requirement.ConsumableId);
                if (consumable == null || !consumable.IsActive)
                {
                    TempData["Error"] = "One or more selected consumables are not available.";
                    return RedirectToAction(nameof(TestTypes), new { filter = "Active" });
                }
            }

            var testTypeId = await _repository.CreateTestType(
                testType.TestCategoryId, testType.TestName, testType.SampleTypeId,
                testType.UnitOfMeasurement, testType.NormalRangeMin, testType.NormalRangeMax,
                testType.TurnaroundTimeMinutes, validRequirements[0].ConsumableId, validRequirements[0].QuantityRequired);
            foreach (var requirement in validRequirements.Skip(1))
                await _repository.CreateTestTypeConsumable(testTypeId, requirement.ConsumableId, requirement.QuantityRequired);
            TempData["Success"] = "Test type created and consumable requirements saved.";

            return RedirectToAction(
                nameof(TestTypes),
                new { filter = "Active" });
        }


        // ==================== GET TEST TYPE BY ID ====================

        [HttpGet]
        public async Task<IActionResult> EditTestType(int id)
        {
            var testType = await _repository.GetTestTypeById(id);

            if (testType == null)
            {
                return NotFound();
            }

            return View(testType);
        }


        // ==================== UPDATE TEST TYPE ====================

        [HttpPost]
        public async Task<IActionResult> EditTestType(
            [Bind("TestTypeId,TestCategoryId,TestName,SampleTypeId,UnitOfMeasurement,NormalRangeMin,NormalRangeMax,TurnaroundTimeMinutes")] TestType testType,
            List<TestTypeConsumable> requirements)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(
                    nameof(TestTypes),
                    new { filter = "Active" });
            }

            var validRequirements = requirements?.Where(r => r.ConsumableId > 0 && r.QuantityRequired > 0).ToList()
                ?? new List<TestTypeConsumable>();
            if (!validRequirements.Any())
            {
                TempData["Error"] = "Select a consumable and enter a quantity greater than zero.";
                return RedirectToAction(nameof(TestTypes), new { filter = "Active" });
            }

            var existingTestType = await _repository.GetTestTypeById(testType.TestTypeId);
            if (existingTestType == null)
            {
                TempData["Error"] = "The test type no longer exists.";
                return RedirectToAction(nameof(TestTypes), new { filter = "Active" });
            }

            foreach (var requirement in validRequirements)
            {
                var consumable = await _repository.GetConsumableById(requirement.ConsumableId);
                if (consumable == null || !consumable.IsActive)
                {
                    TempData["Error"] = "One or more selected consumables are not available.";
                    return RedirectToAction(nameof(TestTypes), new { filter = "Active" });
                }
            }

            await _repository.UpdateTestType(
                testType.TestTypeId, testType.TestCategoryId, testType.TestName,
                testType.SampleTypeId, testType.UnitOfMeasurement, testType.NormalRangeMin,
                testType.NormalRangeMax, testType.TurnaroundTimeMinutes);

            var existingIds = existingTestType.TestTypeConsumable.Select(r => r.TestTypeConsumableId).ToHashSet();
            var submittedIds = validRequirements.Where(r => r.TestTypeConsumableId > 0).Select(r => r.TestTypeConsumableId).ToHashSet();
            foreach (var requirement in validRequirements)
            {
                if (requirement.TestTypeConsumableId > 0 && existingIds.Contains(requirement.TestTypeConsumableId))
                    await _repository.UpdateTestTypeConsumable(requirement.TestTypeConsumableId, testType.TestTypeId, requirement.ConsumableId, requirement.QuantityRequired);
                else
                    await _repository.CreateTestTypeConsumable(testType.TestTypeId, requirement.ConsumableId, requirement.QuantityRequired);
            }
            foreach (var removedId in existingIds.Except(submittedIds))
                await _repository.DeleteTestTypeConsumable(removedId, testType.TestTypeId);
            TempData["Success"] = "Test type and its consumable requirement updated.";

            return RedirectToAction(nameof(TestTypes), new { filter = "Active" });
        }


        // ==================== DELETE TEST TYPE ====================

        [HttpPost]
        public async Task<IActionResult> DeleteTestType(
            int id,
            string filter = "Active")
        {
            await _repository.DeleteTestType(id);

            return RedirectToAction(
                nameof(TestTypes),
                new { filter });
        }


        // ==================== REACTIVATE TEST TYPE ====================

        [HttpPost]
        public async Task<IActionResult> ReactivateTestType(
            int id,
            string filter = "Inactive")
        {
            await _repository.ReactivateTestType(id);

            return RedirectToAction(
                nameof(TestTypes),
                new { filter });
        }

        // ==================== CONSUMABLES ========================================================================
        // ==========================================================================================================

        // ==================== DISPLAY CONSUMABLES ====================

        [HttpGet]
        public async Task<IActionResult> Consumables(
        string filter = "Active",
        string search = "")
        {
            var consumables = await _repository.GetConsumables(
                filter,
                search);

            var suppliers = await _repository.GetSuppliers("Active");

            ViewBag.Filter = filter;
            ViewBag.Search = search;
            ViewBag.Suppliers = suppliers;

            return View(consumables);
        }


        // ==================== CREATE / ADD CONSUMABLES ====================

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateConsumable(Consumable consumable)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Consumables));
            }

            await _repository.CreateConsumable(consumable);

            return RedirectToAction(nameof(Consumables));
        }


        // ==================== GET CONSUMABLE FOR EDIT ====================

        [HttpGet]
        public async Task<IActionResult> EditConsumable(int id)
        {
            var consumable = await _repository.GetConsumableById(id);

            if (consumable == null)
            {
                return NotFound();
            }

            return View(consumable);
        }


        // ==================== UPDATE / EDIT CONSUMABLE ====================

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditConsumable(Consumable consumable)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Consumables));
            }

            await _repository.UpdateConsumable(consumable);

            return RedirectToAction(nameof(Consumables));
        }


        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustConsumableLevel(int id, string field, string mode,
            int? value, int? expectedValue, string? reason, string filter = "Active", string search = "")
        {
            reason = reason?.Trim();
            if (!ModelState.IsValid || id <= 0 || value is null or < 0 || expectedValue is null or < 0 ||
                field is not ("Quantity" or "Reorder") || mode is not ("Set" or "Increase" or "Decrease") ||
                (field == "Reorder" && mode != "Set") ||
                (field == "Quantity" && (string.IsNullOrWhiteSpace(reason) || reason.Length > 100)))
            {
                TempData["Error"] = "Enter a valid non-negative whole number and a stock reason of up to 100 characters.";
                return RedirectToAction(nameof(Consumables), new { filter, search });
            }
            if (!int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var userId))
                return Challenge();
            try
            {
                await _repository.AdjustConsumableLevel(id, field, mode, value.Value, expectedValue.Value, userId, reason);
                TempData["Success"] = field == "Quantity" ? "Quantity on hand saved." : "Reorder level saved.";
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is >= 50100 and <= 50103)
            {
                TempData["Error"] = ex.Message;
            }
            return RedirectToAction(nameof(Consumables), new { filter, search });
        }
        // ==================== DELETE CONSUMABLE ====================

        [HttpPost]
        public async Task<IActionResult> DeleteConsumable(
            int id,
            string filter = "Active")
        {
            await _repository.DeleteConsumable(id);

            return RedirectToAction(
                nameof(Consumables),
                new { filter });
        }


        // ==================== REACTIVATE CONSUMABLE ====================

        [HttpPost]
        public async Task<IActionResult> ReactivateConsumable(
            int id,
            string filter = "Inactive")
        {
            await _repository.ReactivateConsumable(id);

            return RedirectToAction(
                nameof(Consumables),
                new { filter });
        }

        //-----------------------------------------------------------------------------------------------------------------------


        // ==================== SUPPLIERS ====================

        // ==================== DISPLAY SUPPLIERS ====================
        [HttpGet]
        public async Task<IActionResult> Suppliers(
        string filter = "Active",
        string search = "")
        {
            var suppliers = await _repository.GetSuppliers(filter, search);

            ViewBag.Filter = filter;
            ViewBag.Search = search;

            return View(suppliers);
        }

        // ==================== CREATE SUPPLIER ====================
        [HttpPost]
        public async Task<IActionResult> CreateSupplier(Supplier supplier)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Suppliers));
            }

            await _repository.CreateSupplier(supplier);

            return RedirectToAction(nameof(Suppliers));
        }

        // ==================== EDIT SUPPLIER ====================
        [HttpPost]
        public async Task<IActionResult> EditSupplier(Supplier supplier)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Suppliers));
            }

            await _repository.UpdateSupplier(supplier);

            return RedirectToAction(nameof(Suppliers));
        }

        // ==================== DELETE SUPPLIER [SOFT DELETE] ====================
        [HttpPost]
        public async Task<IActionResult> DeleteSupplier(int id, string filter = "Active")
        {
            await _repository.DeleteSupplier(id);

            return RedirectToAction(
                nameof(Suppliers),
                new { filter }
            );
        }


        // ==================== REACTIVATE [SOFT DELETE] ====================
        [HttpPost]
        public async Task<IActionResult> ReactivateSupplier(int id, string filter = "Inactive")
        {
            await _repository.ReactivateSupplier(id);

            return RedirectToAction(
                nameof(Suppliers),
                new { filter }
            );
        }






        // ==================== DISPLAY MANAGERS ====================
        [HttpGet]
        public async Task<IActionResult> ManageManagers(string filter = "Active")
        {
            var managers = await _repository.GetManagers(filter);

            ViewBag.Filter = filter;
            ViewBag.Provinces = await _repository.GetProvinces();

            return View(managers);
        }


        // ==================== CREATE MANAGER ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateManager(LabManager manager)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(ManageManagers));
            }

            var temporaryPassword = PasswordHelper.GenerateTemporaryPassword();
            await _repository.CreateManager(manager, temporaryPassword);
            await SendWelcomeEmail(manager.User, temporaryPassword);

            return RedirectToAction(nameof(ManageManagers));
        }

        // ==================== EDIT MANAGER (GET) ====================
        [HttpGet]
        public async Task<IActionResult> EditManager(int id)
        {
            var manager = await _repository.GetManagerById(id);

            if (manager == null)
            {
                return NotFound();
            }

            return View(manager);
        }

        // ==================== EDIT MANAGER (POST) ====================
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditManager(LabManager manager)
        {
            if (!ModelState.IsValid)
            {
                return View(manager);
            }

            await _repository.UpdateManager(manager);

            return RedirectToAction(nameof(ManageManagers));
        }

        // ==================== DELETE MANAGER ====================
        [HttpPost]
        public async Task<IActionResult> DeleteManager(int id, string filter = "Active")
        {
            await _repository.DeleteManager(id);

            return RedirectToAction(
                nameof(ManageManagers),
                new { filter }
            );
        }
        // ==================== REACTIVATE MANAGER ====================
        [HttpPost]
        public async Task<IActionResult> ReactivateManager(int id, string filter = "Inactive")
        {
            await _repository.ReactivateManager(id);

            return RedirectToAction(
                nameof(ManageManagers),
                new { filter }
            );
        }
        //==========================================================================================================================



        // ==================== DOCTORS ========================================================================
        //==============================================================


        // ==================== DISPLAY DOCTORS ====================
        [HttpGet]
        public async Task<IActionResult> Doctors(string filter = "Active")
        {
            var doctors = await _repository.GetDoctors(filter);

            ViewBag.Filter = filter;
            ViewBag.Provinces = await _repository.GetProvinces();

            return View(doctors);
        }


        // ==================== CREATE/ADD DOCTORS ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDoctor(Doctor doctor)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Doctors));
            }

            if (!ValidationHelper.IsSouthAfricanIdMatchDob(
                doctor.User.SaIdNumber,
                doctor.User.DateOfBirth))
            {
                TempData["Error"] = "SA ID Number does not match the Date of Birth.";

                return RedirectToAction(nameof(Doctors));
            }

            var temporaryPassword = PasswordHelper.GenerateTemporaryPassword();
            await _repository.CreateDoctor(doctor, temporaryPassword);
            await SendWelcomeEmail(doctor.User, temporaryPassword);

            return RedirectToAction(nameof(Doctors));
        }

        // ==================== UPDATE/EDIT DOCTORS ==================== [GET]
        [HttpGet]
        public async Task<IActionResult> EditDoctor(int id)
        {
            var doctor = await _repository.GetDoctorById(id);

            if (doctor == null)
            {
                return NotFound();
            }

            return View(doctor);
        }

        // ==================== UPDATE/EDIT DOCTORS ==================== [POST]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDoctor(Doctor doctor)
        {
            if (!ModelState.IsValid)
            {
                return View(doctor);
            }
            if (!ValidationHelper.IsSouthAfricanIdMatchDob(
                        doctor.User.SaIdNumber,
                        doctor.User.DateOfBirth))
            {
                TempData["Error"] = "SA ID Number does not match the Date of Birth.";

                return RedirectToAction(nameof(Doctors));
            }

            await _repository.UpdateDoctor(doctor);

            return RedirectToAction(nameof(Doctors));
        }

        // ==================== DELETE DOCTORS ====================
        [HttpPost]
        public async Task<IActionResult> DeleteDoctor(int id, string filter = "Active")
        {
            await _repository.DeleteDoctor(id);

            return RedirectToAction(nameof(Doctors), new { filter });
        }


        // ==================== REACTIVATE DOCTOR ====================
        [HttpPost]
        public async Task<IActionResult> ReactivateDoctor(int id)
        {
            await _repository.ReactivateDoctor(id);

            return RedirectToAction(nameof(Doctors));
        }

        //================================================================================================================

        // ==================== DISPLAY TECHNICIANS ====================
        [HttpGet]
        public async Task<IActionResult> Technicians(string filter = "Active")
        {
            var technicians = (await _repository.GetTechnicians(filter)).ToList();
            var testTypes = (await _repository.GetTestTypes("Active")).ToList();
            var assignmentTasks = technicians.Select(async technician => new
            {
                technician.LabTechnicianId,
                TestTypeIds = (await _repository.GetTechnicianTestTypes(technician.LabTechnicianId))
                    .Select(assignment => assignment.TestTypeId)
                    .ToHashSet()
            });
            var assignments = (await Task.WhenAll(assignmentTasks))
                .ToDictionary(item => item.LabTechnicianId, item => item.TestTypeIds);

            ViewBag.Filter = filter;
            ViewBag.TestTypes = testTypes;
            ViewBag.TechnicianTestTypes = assignments;
            ViewBag.Provinces = await _repository.GetProvinces();

            return View(technicians);
        }

        // ==================== CREATE TECHNICIAN ====================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTechnician(LabTechnician technician, int[] testTypeIds)
        {
            if (!ModelState.IsValid)
            {
                foreach (var item in ModelState)
                {
                    Console.WriteLine($"Field: {item.Key}");

                    foreach (var error in item.Value.Errors)
                    {
                        Console.WriteLine($"Error: {error.ErrorMessage}");
                    }
                }

                return RedirectToAction(nameof(Technicians));
            }
            if (!ValidationHelper.IsSouthAfricanIdMatchDob(
            technician.User.SaIdNumber,
            technician.User.DateOfBirth))
            {
                TempData["Error"] = "The SA ID Number does not match the Date of Birth.";

                return RedirectToAction(nameof(Technicians));
            }

            var selectedIds = (testTypeIds ?? Array.Empty<int>()).Where(id => id > 0).Distinct().ToArray();
            var activeIds = (await _repository.GetTestTypes("Active"))
                .Select(testType => testType.TestTypeId)
                .ToHashSet();
            if (selectedIds.Any(id => !activeIds.Contains(id)))
            {
                TempData["Error"] = "One or more selected test types are not active.";
                return RedirectToAction(nameof(Technicians));
            }

            if (selectedIds.Length == 0)
            {
                TempData["Error"] = "Assign at least one test type to the technician.";
                return RedirectToAction(nameof(Technicians));
            }

            var temporaryPassword = PasswordHelper.GenerateTemporaryPassword();
            try
            {
                await _repository.CreateTechnician(technician, temporaryPassword, selectedIds);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 2812)
            {
                _logger.LogError(ex, "Technician creation procedure is missing or unavailable.");
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    errors = new Dictionary<string, string[]>
                    {
                        [""] = new[] { "Technician creation is not configured in the database. Apply Staff_AccountCreation.sql and TestType_TechnicianAssignments.sql, then try again." }
                    }
                });
            }
            catch (Microsoft.Data.SqlClient.SqlException ex)
            {
                _logger.LogError(ex, "Database failed while creating technician {Email}.", technician.User.Email);
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    errors = new Dictionary<string, string[]>
                    {
                        [""] = new[] { "The technician could not be saved because the database rejected the request. Check the database setup and try again." }
                    }
                });
            }


            await SendWelcomeEmail(technician.User, temporaryPassword);

            return RedirectToAction(nameof(Technicians));
        }
        // ==================== EDIT TECHNICIAN ====================
        [HttpGet]
        public async Task<IActionResult> EditTechnician(int id)
        {
            var technician = await _repository.GetTechnicianById(id);

            if (technician == null)
            {
                return NotFound();
            }

            return View(technician);
        }

        // ==================== UPDATE TECHNICIAN ====================
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTechnician(LabTechnician technician)
        {
            if (!ModelState.IsValid)
            {
                return View(technician);
            }
            if (!ValidationHelper.IsSouthAfricanIdMatchDob(
              technician.User.SaIdNumber,
              technician.User.DateOfBirth))
            {
                TempData["Error"] = "The SA ID Number does not match the Date of Birth.";

                return RedirectToAction(nameof(Technicians));
            }

            await _repository.UpdateTechnician(technician);


            return RedirectToAction(nameof(Technicians));
        }

        // ==================== DELETE TECHNICIAN ====================
        [HttpPost]
        public async Task<IActionResult> DeleteTechnician(int id, string filter = "Active")
        {
            await _repository.DeleteTechnician(id);

            return RedirectToAction(
                nameof(Technicians),
                new { filter }
            );
        }

        // ==================== REACTIVATE TECHNICIAN ====================
        [HttpPost]
        public async Task<IActionResult> ReactivateTechnician(int id, string filter = "Inactive")
        {
            await _repository.ReactivateTechnician(id);

            return RedirectToAction(
                nameof(Technicians),
                new { filter }
            );
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTechnicianTestTypes(int technicianId, int[] testTypeIds, string filter = "Active")
        {
            var technician = await _repository.GetTechnicianById(technicianId);
            if (technician?.User?.IsActive != true)
            {
                TempData["Error"] = "Only active technicians can be assigned test types.";
                return RedirectToAction(nameof(Technicians), new { filter });
            }

            var selectedIds = (testTypeIds ?? Array.Empty<int>()).Where(id => id > 0).Distinct().ToArray();
            var activeIds = (await _repository.GetTestTypes("Active"))
                .Select(testType => testType.TestTypeId)
                .ToHashSet();
            if (selectedIds.Any(id => !activeIds.Contains(id)))
            {
                TempData["Error"] = "One or more selected test types are not active.";
                return RedirectToAction(nameof(Technicians), new { filter });
            }


            if (!ModelState.IsValid || selectedIds.Length == 0)
            {
                TempData["Error"] = "Assign at least one active test type.";
                return RedirectToAction(nameof(Technicians), new { filter });
            }
            try
            {
                await _repository.ReplaceTechnicianTestTypes(technicianId, selectedIds);
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number is >= 50100 and <= 50103)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Technicians), new { filter });
            }
            TempData["Success"] = "Technician test-type assignments updated.";
            return RedirectToAction(nameof(Technicians), new { filter });
        }
        //================================================================================================================

        [HttpGet]
        public IActionResult ManagerReports(DateTime? startDate, DateTime? endDate, string reportType = "tests", string orderStatus = "All", bool lowStockOnly = false)
        {
            var today = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(2)).Date;
            ViewBag.ReportType = reportType; ViewBag.OrderStatus = orderStatus; ViewBag.LowStockOnly = lowStockOnly;
            ViewBag.ReportStart = startDate?.Date ?? today.AddMonths(-1);
            ViewBag.ReportEnd = endDate?.Date ?? today;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> PreviewReport(string reportType, DateTime? startDate, DateTime? endDate,
            string orderStatus = "All", bool lowStockOnly = false)
        {
            var isEmbeddedPreview = string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest",
                StringComparison.OrdinalIgnoreCase);
            var statuses = new[] { "All", "Ordered", "Partially Received", "Received", "Cancelled" };
            var dated = reportType is "tests" or "orders";
            if (!ModelState.IsValid || reportType is not ("tests" or "orders" or "stock") ||
                (reportType == "orders" && !statuses.Contains(orderStatus)) ||
                (dated && (!startDate.HasValue || !endDate.HasValue || startDate.Value == default ||
                    startDate.Value.Date > endDate.Value.Date || endDate.Value.Date == DateTime.MaxValue.Date)))
            {
                if (isEmbeddedPreview)
                    return BadRequest("Choose a valid report and date range. The end date must be on or after the start date.");
                TempData["ReportError"] = "Choose a valid report and date range. The end date must be on or after the start date.";
                return RedirectToAction(nameof(ManagerReports), new { reportType, startDate, endDate, orderStatus, lowStockOnly });
            }

            var culture = System.Globalization.CultureInfo.GetCultureInfo("en-ZA");
            string Date(DateTime? value) => value?.ToString("dd MMM yyyy", culture) ?? "Not applicable";
            var generatedAt = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(2));
            List<string[]> rows;
            string[] headings;
            string title;
            string criteria;
            string summary;

            if (reportType == "tests")
            {
                IEnumerable<TestsPerformedDetail> details;
                try
                {
                    details = await _repository.GetTestsPerformedDetails(
                        startDate!.Value.Date, endDate!.Value.Date.AddDays(1));
                }
                catch (Microsoft.Data.SqlClient.SqlException exception) when (exception.Number == 2812)
                {
                    if (isEmbeddedPreview)
                        return StatusCode(StatusCodes.Status500InternalServerError,
                            "The detailed test report is not installed yet. Run Database/ManagerReport_Details.sql on the application database.");
                    TempData["ReportError"] = "The detailed test report is not installed yet. Run Database/ManagerReport_Details.sql on the application database.";
                    return RedirectToAction(nameof(ManagerReports), new { reportType, startDate, endDate, orderStatus, lowStockOnly });
                }
                var tests = details
                    .OrderBy(row => row.CategoryName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(row => row.TestName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(row => row.CompletionDateTime)
                    .ToList();
                title = "Tests Performed by Category";
                criteria = $"Completion dates: {Date(startDate)} - {Date(endDate)} (inclusive).";
                summary = tests.Count == 0 ? "No completed tests were found for the selected date range."
                    : $"Completed tests: {tests.Count:N0}. Categories: {tests.Select(test => test.CategoryName).Distinct(StringComparer.OrdinalIgnoreCase).Count():N0}.";
                headings = new[] { "Test category", "Test type", "Completed on" };
                rows = tests.Select(test => new[]
                {
                    test.CategoryName,
                    test.TestName,
                    Date(test.CompletionDateTime)
                }).ToList();
            }
            else if (reportType == "orders")
            {
                var orders = (await _repository.GetStockOrders("All", ""))
                    .Where(order => order.OrderDate.HasValue && order.OrderDate.Value >= startDate!.Value.Date
                        && order.OrderDate.Value < endDate!.Value.Date.AddDays(1))
                    .Where(order => orderStatus == "All" || OrderStatusLabels.Display(order.Status) == orderStatus)
                    .OrderBy(order => order.OrderDate).ThenBy(order => order.OrderNumber, StringComparer.OrdinalIgnoreCase).ToList();
                title = "Stock Orders Report";
                criteria = $"Order dates: {Date(startDate)} - {Date(endDate)} (inclusive). Status: {orderStatus}.";
                summary = orders.Count == 0 ? "No stock orders were found for the selected criteria."
                    : $"Orders: {orders.Count:N0}. Ordered: {orders.Count(order => OrderStatusLabels.Display(order.Status) == "Ordered"):N0}. Partially received: {orders.Count(order => OrderStatusLabels.Display(order.Status) == "Partially Received"):N0}. Received: {orders.Count(order => OrderStatusLabels.Display(order.Status) == "Received"):N0}. Cancelled: {orders.Count(order => OrderStatusLabels.Display(order.Status) == "Cancelled"):N0}.";
                headings = new[] { "Order number", "Supplier", "Ordered on", "Item lines", "Status", "Received on" };
                rows = orders.Select(order => new[]
                {
                    order.OrderNumber, order.Supplier?.SupplierName ?? "Unknown", Date(order.OrderDate),
                    order.StockOrderItem.Count.ToString("N0", culture), OrderStatusLabels.Display(order.Status), Date(order.CompletionDate)
                }).ToList();
            }
            else
            {
                var stock = (await _repository.GetConsumables("Active"))
                    .Where(consumable => !lowStockOnly || consumable.QuantityOnHand <= consumable.ReorderLevel * 1.1m)
                    .OrderBy(consumable => consumable.Supplier?.SupplierName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(consumable => consumable.Name, StringComparer.OrdinalIgnoreCase).ToList();
                title = "Current Consumable Stock";
                criteria = $"Current stock snapshot. Selection: {(lowStockOnly ? "near or below reorder level (within 10%)" : "all active consumables")}.";
                summary = stock.Count == 0 ? "No consumable stock records were found for the selected criteria."
                    : $"Stock items: {stock.Count:N0}. Near or below reorder level: {stock.Count(consumable => consumable.QuantityOnHand <= consumable.ReorderLevel * 1.1m):N0}.";
                headings = new[] { "Consumable", "Supplier", "Quantity on hand", "Reorder level", "Stock status" };
                rows = stock.Select(consumable => new[]
                {
                    consumable.Name, consumable.Supplier?.SupplierName ?? "Unknown",
                    consumable.QuantityOnHand.ToString("N0", culture), consumable.ReorderLevel.ToString("N0", culture),
                    consumable.QuantityOnHand <= consumable.ReorderLevel ? "At/below reorder" : consumable.QuantityOnHand <= consumable.ReorderLevel * 1.1m ? "Near reorder" : "Available"
                }).ToList();
            }

            ViewBag.ReportTitle = title;
            ViewBag.Criteria = criteria;
            ViewBag.Summary = summary;
            ViewBag.Headings = headings;
            ViewBag.GeneratedAt = generatedAt;
            ViewBag.GeneratedBy = CurrentReportGenerator();
            ViewBag.ReportType = reportType;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.OrderStatus = orderStatus;
            ViewBag.LowStockOnly = lowStockOnly;
            return View("ReportPreview", rows);
        }

        [HttpGet]
        public async Task<IActionResult> TestsPerformedReport(DateTime startDate, DateTime endDate)
        {
            if (!ModelState.IsValid || startDate == default || endDate == default || startDate.Date > endDate.Date || endDate.Date == DateTime.MaxValue.Date)
            {
                TempData["ReportError"] = "Select a valid start and end date.";
                return RedirectToAction(nameof(ManagerReports), new { startDate = startDate.ToString("yyyy-MM-dd"), endDate = endDate.ToString("yyyy-MM-dd") });
            }

            List<TestsPerformedDetail> tests;
            try
            {
                tests = (await _repository.GetTestsPerformedDetails(
                    startDate.Date,
                    endDate.Date.AddDays(1))).ToList();
            }
            catch (Microsoft.Data.SqlClient.SqlException exception) when (exception.Number == 2812)
            {
                TempData["ReportError"] = "The detailed test report is not installed yet. Run Database/ManagerReport_Details.sql on the application database.";
                return RedirectToAction(nameof(ManagerReports), new { startDate = startDate.ToString("yyyy-MM-dd"), endDate = endDate.ToString("yyyy-MM-dd") });
            }

            var pdf = TestsPerformedPdf.Generate(tests, startDate.Date, endDate.Date, DateTimeOffset.UtcNow,
                CurrentReportGenerator());
            return File(pdf, "application/pdf");
        }

        [HttpGet]
        public async Task<IActionResult> DownloadReport(string reportType, DateTime? startDate, DateTime? endDate,
            string orderStatus = "All", bool lowStockOnly = false)
        {
            var statuses = new[] { "All", "Ordered", "Partially Received", "Received", "Cancelled" };
            var dated = reportType is "tests" or "orders";
            if (!ModelState.IsValid || reportType is not ("tests" or "orders" or "stock") ||
                (reportType == "orders" && !statuses.Contains(orderStatus)) ||
                (dated && (!startDate.HasValue || !endDate.HasValue || startDate.Value == default ||
                    startDate.Value.Date > endDate.Value.Date || endDate.Value.Date == DateTime.MaxValue.Date)))
            {
                TempData["ReportError"] = "Choose a valid report and date range. The end date must be on or after the start date.";
                return RedirectToAction(nameof(ManagerReports), new { reportType, startDate, endDate, orderStatus, lowStockOnly });
            }
            if (reportType == "tests")
                return await TestsPerformedReport(startDate!.Value, endDate!.Value);

            var culture = System.Globalization.CultureInfo.GetCultureInfo("en-ZA");
            var now = DateTimeOffset.UtcNow;
            var generatedBy = CurrentReportGenerator();
            byte[] pdf;
            if (reportType == "orders")
            {
                var orders = (await _repository.GetStockOrders("All", ""))
                    .Where(x => x.OrderDate.HasValue && x.OrderDate.Value >= startDate!.Value.Date
                        && x.OrderDate.Value < endDate!.Value.Date.AddDays(1))
                    .Where(x => orderStatus == "All" || OrderStatusLabels.Display(x.Status) == orderStatus)
                    .OrderBy(x => x.OrderDate).ThenBy(x => x.OrderNumber, StringComparer.OrdinalIgnoreCase).ToList();
                string Date(DateTime? value) => value?.ToString("dd MMM yyyy", culture) ?? "—";
                pdf = ManagementReportPdf.Generate("Stock Orders Report",
                    $"Order dates: {Date(startDate)} – {Date(endDate)} (inclusive). Status: {orderStatus}. Sorted by order date, then order number. Statuses are current at generation.",
                    $"Orders: {orders.Count:N0} • Ordered: {orders.Count(x => OrderStatusLabels.Display(x.Status) == "Ordered"):N0} • Partially received: {orders.Count(x => OrderStatusLabels.Display(x.Status) == "Partially Received"):N0} • Received: {orders.Count(x => OrderStatusLabels.Display(x.Status) == "Received"):N0} • Cancelled: {orders.Count(x => OrderStatusLabels.Display(x.Status) == "Cancelled"):N0}",
                    new[] { "Order number", "Supplier", "Ordered on", "Item lines", "Status", "Received on" },
                    orders.Select(x => new[] { x.OrderNumber, x.Supplier?.SupplierName ?? "Unknown", Date(x.OrderDate),
                        x.StockOrderItem.Count.ToString("N0", culture), OrderStatusLabels.Display(x.Status), Date(x.CompletionDate) }), now, generatedBy);
            }
            else
            {
                var stock = (await _repository.GetConsumables("Active"))
                    .Where(x => !lowStockOnly || x.QuantityOnHand <= x.ReorderLevel * 1.1m)
                    .OrderBy(x => x.Supplier?.SupplierName, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ToList();
                pdf = ManagementReportPdf.Generate("Current Consumable Stock",
                    $"Current snapshot, not historical stock. Active consumables only. Selection: {(lowStockOnly ? "near/below reorder level (within 10%)" : "all stock")}. Sorted by supplier, then consumable.",
                    $"Stock items: {stock.Count:N0} • Near/below reorder level: {stock.Count(x => x.QuantityOnHand <= x.ReorderLevel * 1.1m):N0}",
                    new[] { "Consumable", "Supplier", "Quantity on hand", "Reorder level", "Stock status" },
                    stock.Select(x => new[] { x.Name, x.Supplier?.SupplierName ?? "Unknown",
                        x.QuantityOnHand.ToString("N0", culture), x.ReorderLevel.ToString("N0", culture),
                        x.QuantityOnHand <= x.ReorderLevel ? "At/below reorder" : x.QuantityOnHand <= x.ReorderLevel * 1.1m ? "Near reorder" : "Available" }), now, generatedBy);
            }
            return File(pdf, "application/pdf");
        }
        public IActionResult Profile() => RedirectToAction("Profile", "Account");


    }




}





