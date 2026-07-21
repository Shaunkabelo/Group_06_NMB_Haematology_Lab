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

        
    }
}
