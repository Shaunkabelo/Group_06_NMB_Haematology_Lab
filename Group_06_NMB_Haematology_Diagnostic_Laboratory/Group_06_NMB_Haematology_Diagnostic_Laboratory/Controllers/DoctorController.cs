using Microsoft.AspNetCore.Mvc;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers
{
	public class DoctorController : Controller
	{
		public IActionResult Dashboard() => View();

		public IActionResult Patients() => View();

		public IActionResult Requests() => View();

		public IActionResult Results() => View();

		public IActionResult Alerts() => View();

		public IActionResult Reports() => View();

		public IActionResult Account() => View();

		public IActionResult CreateRequest() => View();

		[HttpGet]
		public IActionResult RequestDetails(string id)
		{
			ViewData["RequestId"] = id;
			return View();
		}

		//[HttpGet]
		//public IActionResult GetPatientBySouthAfricanId(string id)
		//{
		//    var patient = PatientStore.Patients.FirstOrDefault(p => p.SouthAfricanId == id);

		//    if (patient == null)
		//        return NotFound();

		//    return Json(new
		//    {
		//        firstName = patient.FirstName,
		//        lastName = patient.LastName,
		//        physicalAddress = patient.PhysicalAddress,
		//        cellphone = patient.Cellphone,
		//        email = patient.Email,
		//        knownMedicalConditions = patient.KnownMedicalConditions,
		//        allergies = patient.Allergies,
		//        medication = patient.Medication
		//    });
		//}
	}
}
