using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Helpers;
using Group_06_NMB_Haematology_Diagnostic_Laboratory.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers
{
    [Authorize(Roles = "LabManager,Admin")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public class LabManagerController : Controller
    {
        private readonly ILabManagerRepository _repository;

        public LabManagerController(ILabManagerRepository repository)
        {
            _repository = repository;
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
            if (supplierId <= 0 || selectedIds.Count == 0)
            {
                TempData["ErrorMessage"] = "Select a supplier and at least one consumable.";
                return RedirectToAction(nameof(Orders));
            }

            var supplier = await _repository.GetSupplierById(supplierId);
            var consumables = (await _repository.GetConsumables("Active"))
                .ToDictionary(c => c.ConsumableId);

            if (supplier == null || selectedIds.Any(id => !consumables.ContainsKey(id)))
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

            var safeOrderNumber = string.IsNullOrWhiteSpace(orderNumber)
                ? $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8]}"
                : orderNumber.Trim();

            var stockOrderId = await _repository.CreateStockOrder(new StockOrder
            {
                SupplierId = supplierId,
                OrderNumber = safeOrderNumber,
                OrderDate = DateTime.Now
            });

            foreach (var item in items)
            {
                await _repository.CreateStockOrderItem(stockOrderId, item.ConsumableId, item.Quantity);
            }

            TempData["SuccessMessage"] = $"Order {safeOrderNumber} was created.";
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReceiveOrderItem(int stockOrderItemId, int stockOrderId)
        {
            if (stockOrderItemId <= 0)
            {
                TempData["ErrorMessage"] = "Invalid order item.";
                return RedirectToAction(nameof(Orders));
            }

            await _repository.ReceiveStockOrderItem(stockOrderItemId);
            TempData["SuccessMessage"] = "Order item received and stock updated.";
            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrderItem(int stockOrderItemId, string? cancelReason)
        {
            if (stockOrderItemId <= 0 || string.IsNullOrWhiteSpace(cancelReason))
            {
                TempData["ErrorMessage"] = "A reason is required to cancel an order item.";
                return RedirectToAction(nameof(Orders));
            }

            await _repository.CancelStockOrderItem(stockOrderItemId, cancelReason.Trim());
            TempData["SuccessMessage"] = "Order item cancelled.";
            return RedirectToAction(nameof(Orders));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int stockOrderId, string? cancelReason)
        {
            if (stockOrderId <= 0 || string.IsNullOrWhiteSpace(cancelReason))
            {
                TempData["ErrorMessage"] = "A reason is required to cancel an order.";
                return RedirectToAction(nameof(Orders));
            }

            await _repository.CancelStockOrder(stockOrderId, cancelReason.Trim());
            TempData["SuccessMessage"] = "Order cancelled.";
            return RedirectToAction(nameof(Orders));
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
    string search = "")
        {
            var testTypes = await _repository.GetTestTypes(filter, search);

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
            TempData["Success"] = "Test type created and consumable stock allocated.";

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

        [HttpPost]
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

        [HttpPost]
        public async Task<IActionResult> EditConsumable(Consumable consumable)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Consumables));
            }

            await _repository.UpdateConsumable(consumable);

            return RedirectToAction(nameof(Consumables));
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

            return View(managers);
        }


        // ==================== CREATE MANAGER ====================
        [HttpPost]
        public async Task<IActionResult> CreateManager(LabManager manager)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(ManageManagers));
            }

            await _repository.CreateManager(manager);

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
        [HttpPost]
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

            return View(doctors);
        }


        // ==================== CREATE/ADD DOCTORS ====================
        [HttpPost]
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

            await _repository.CreateDoctor(doctor);

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
        [HttpPost]
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

            return View(technicians);
        }

        // ==================== CREATE TECHNICIAN ====================
        [HttpPost]
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

            var technicianId = await _repository.CreateTechnician(technician);
            await _repository.ReplaceTechnicianTestTypes(technicianId, selectedIds);

            TempData["Success"] = "Technician created and test-type assignments saved.";

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
        [HttpPost]
        public async Task<IActionResult> EditTechnician(LabTechnician technician, int[] testTypeIds)
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

            var selectedIds = (testTypeIds ?? Array.Empty<int>()).Where(id => id > 0).Distinct().ToArray();
            var activeIds = (await _repository.GetTestTypes("Active"))
                .Select(testType => testType.TestTypeId)
                .ToHashSet();
            if (selectedIds.Length == 0)
            {
                TempData["Error"] = "Assign at least one test type to the technician.";
                return RedirectToAction(nameof(Technicians));
            }
            if (selectedIds.Any(id => !activeIds.Contains(id)))
            {
                TempData["Error"] = "One or more selected test types are not active.";
                return RedirectToAction(nameof(Technicians));
            }

            await _repository.UpdateTechnician(technician);
            await _repository.ReplaceTechnicianTestTypes(technician.LabTechnicianId, selectedIds);

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

        [HttpPost]
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

            await _repository.ReplaceTechnicianTestTypes(technicianId, selectedIds);
            TempData["Success"] = "Technician test-type assignments updated.";
            return RedirectToAction(nameof(Technicians), new { filter });
        }
        //================================================================================================================

        [HttpGet]
        public IActionResult ManagerReports() => View();

        [HttpGet]
        public async Task<IActionResult> TestsPerformedReport(DateTime startDate, DateTime endDate)
        {
            if (startDate == default || endDate == default || startDate.Date > endDate.Date)
            {
                TempData["ReportError"] = "Select a valid start and end date.";
                return RedirectToAction(nameof(ManagerReports));
            }

            var categories = (await _repository.GetTestsPerformedByCategory(
                startDate.Date,
                endDate.Date.AddDays(1)))
                .OrderByDescending(row => row.TotalTests)
                .ThenBy(row => row.CategoryName)
                .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(45);
                    page.Header().Column(column =>
                    {
                        column.Item().Text("NMB-HLabSys").FontSize(18).Bold().FontColor(Colors.Blue.Medium);
                        column.Item().Text("Tests Performed by Category").FontSize(24).Bold();
                        column.Item().Text($"Reporting period: {startDate:dd MMM yyyy} to {endDate:dd MMM yyyy}").FontSize(10).FontColor(Colors.Grey.Darken1);
                    });
                    page.Content().PaddingTop(24).Column(column =>
                    {
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(1);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Medium).Padding(8).Text("Test category").FontColor(Colors.White).Bold();
                                header.Cell().Background(Colors.Blue.Medium).Padding(8).AlignRight().Text("Total tests").FontColor(Colors.White).Bold();
                            });
                            foreach (var category in categories)
                            {
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Text(category.CategoryName);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(8).AlignRight().Text(category.TotalTests.ToString());
                            }
                        });
                        column.Item().PaddingTop(18).Text($"Total tests performed: {categories.Sum(row => row.TotalTests)}").Bold();
                    });
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Generated ");
                        text.Span(DateTime.Now.ToString("dd MMM yyyy HH:mm"));
                    });
                });
            });

            return File(document.GeneratePdf(), "application/pdf",
                $"tests-performed-{startDate:yyyyMMdd}-{endDate:yyyyMMdd}.pdf");
        }

        public IActionResult Profile() => View();


    }




}

