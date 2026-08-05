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

        public IActionResult TestTypes() => View();

        public IActionResult Consumables() => View();

        public IActionResult Orders() => View();

        public IActionResult Suppliers() => View();






        // ==================== DISPLAY MANAGERS ====================
        [HttpGet]
        public async Task<IActionResult> ManageManagers()
        {
            var managers = await _repository.GetManagers();

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
        public async Task<IActionResult> DeleteManager(int id)
        {
            await _repository.DeleteManager(id);

            return RedirectToAction(nameof(ManageManagers));
        }
        //==========================================================================================================================



        // ==================== DOCTORS ========================================================================
        //==============================================================


        // ==================== DISPLAY DOCTORS ====================
        [HttpGet]
        public async Task<IActionResult> Doctors()
        {
            var doctors = await _repository.GetDoctors();

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
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            await _repository.DeleteDoctor(id);

            return RedirectToAction(nameof(Doctors));
        }

        //================================================================================================================

        // ==================== DISPLAY TECHNICIANS ====================
        [HttpGet]
        public async Task<IActionResult> Technicians()
        {
            var technicians = await _repository.GetTechnicians();

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
        public async Task<IActionResult> DeleteTechnician(int id)
        {
            await _repository.DeleteTechnician(id);

            return RedirectToAction(nameof(Technicians));
        }


        public IActionResult ManagerReports() => View();

        public IActionResult Profile() => View();


    }



}

