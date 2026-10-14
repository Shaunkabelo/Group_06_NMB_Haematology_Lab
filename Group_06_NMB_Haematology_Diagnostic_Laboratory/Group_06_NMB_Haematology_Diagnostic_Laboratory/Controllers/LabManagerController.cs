using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Group_06_NMB_Haematology_Diagnostic_Laboratory.Controllers;

[Authorize(Roles = "LabManager")]
public class LabManagerController : Controller
{
    [HttpGet]
    public IActionResult Dashboard() => View();
}
