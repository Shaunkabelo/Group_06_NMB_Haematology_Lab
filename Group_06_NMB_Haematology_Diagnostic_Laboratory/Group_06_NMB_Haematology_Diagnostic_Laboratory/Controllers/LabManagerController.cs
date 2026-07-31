using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository;
using Microsoft.AspNetCore.Mvc;

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

        public IActionResult ManageManagers() => View();

        //            ==== DISPLAY DOCTOR ====
        [HttpGet]
        public async Task<IActionResult> Doctors()
        {
            var doctors = await _repository.GetDoctors();

            return View(doctors);
        }
        //            ==== INSERT DOCTOR =====
        [HttpPost]
        public async Task<IActionResult> CreateDoctor(Doctor doctor)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction(nameof(Doctors));
            }

            await _repository.CreateDoctor(doctor);

            return RedirectToAction(nameof(Doctors));
        }

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

        [HttpPost]
        public async Task<IActionResult> EditDoctor(Doctor doctor)
        {
            if (!ModelState.IsValid)
            {
                return View(doctor);
            }

            await _repository.UpdateDoctor(doctor);

            return RedirectToAction(nameof(Doctors));
        }
        [HttpPost]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            await _repository.DeleteDoctor(id);

            return RedirectToAction(nameof(Doctors));
        }

        public IActionResult Technicians()
        {
            return View();
        }

        public IActionResult ManagerReports() => View();

        public IActionResult Profile() => View();


    }



}

