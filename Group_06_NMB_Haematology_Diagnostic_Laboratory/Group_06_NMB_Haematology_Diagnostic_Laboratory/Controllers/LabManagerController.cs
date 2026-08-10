using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Microsoft.AspNetCore.Mvc;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Helpers;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers
{
    public class LabManagerController : Controller
    {
        private readonly ILabManagerRepository _repository;

        public LabManagerController(ILabManagerRepository repository)
        {
            _repository = repository;
        }
        public IActionResult Dashboard() => View();

        public IActionResult Categories() => View();

        

        
        public IActionResult Orders() => View();

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

            ViewBag.Filter = filter;
            ViewBag.Search = search;

            ViewBag.Categories = categories;
            ViewBag.SampleTypes = sampleTypes;

            return View(testTypes);
        }


        // ==================== CREATE TEST TYPE ====================

        [HttpPost]
        public async Task<IActionResult> CreateTestType(TestType testType)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(
                    nameof(TestTypes),
                    new { filter = "Active" });
            }

            await _repository.CreateTestType(testType);

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
        public async Task<IActionResult> EditTestType(TestType testType)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(
                    nameof(TestTypes),
                    new { filter = "Active" });
            }

            await _repository.UpdateTestType(testType);

            return RedirectToAction(
                nameof(TestTypes),
                new { filter = "Active" });
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
            var technicians = await _repository.GetTechnicians(filter);

            ViewBag.Filter = filter;

            return View(technicians);
        }

        // ==================== CREATE TECHNICIAN ====================
        [HttpPost]
        public async Task<IActionResult> CreateTechnician(LabTechnician technician)
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

            await _repository.CreateTechnician(technician);

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
        //================================================================================================================

        public IActionResult ManagerReports() => View();

        public IActionResult Profile() => View();


    }




}

