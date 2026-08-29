using Microsoft.AspNetCore.Mvc;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers
{
	public class AdminController : Controller
	{
		public IActionResult Dashboard()
		{
			return View();
		}

		public IActionResult Condition()
		{
			return View();
		}

		public IActionResult Allergies()
		{
			return View();
		}

		public IActionResult Medication()
		{
			return View();
		}

		public IActionResult Users()
		{
			return View();
		}

		public IActionResult Audit()
		{
			return View();
		}
	}
}
