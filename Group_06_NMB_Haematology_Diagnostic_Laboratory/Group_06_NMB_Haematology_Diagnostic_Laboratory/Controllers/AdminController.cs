using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers
{
	[Authorize(Roles = "Admin")]
	public class AdminController : Controller
	{
		public IActionResult Dashboard()
		{
			return AdminPage("Admin Dashboard", "Overview of the laboratory administration workspace.");
		}

		public IActionResult Condition()
		{
			return AdminPage("Conditions", "Manage the medical conditions available in patient records.");
		}

		public IActionResult Allergies()
		{
			return AdminPage("Allergies", "Manage the allergies available in patient records.");
		}

		public IActionResult Medication()
		{
			return AdminPage("Medications", "Manage the medications available in patient records.");
		}

		public IActionResult Users()
		{
			return AdminPage("System Users", "Review and manage accounts with access to the system.");
		}

		public IActionResult Audit()
		{
			return AdminPage("Audit Log", "Review administrative and system activity.");
		}

		private IActionResult AdminPage(string title, string description)
		{
			ViewData["Title"] = title;
			ViewData["Description"] = description;
			return View("Page");
		}
	}
}
