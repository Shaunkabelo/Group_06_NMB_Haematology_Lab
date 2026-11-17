using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.ViewModels
{
    public class TechProfileModel
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Cellphone { get; set; }
        public string? SaIdNumber { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? EmployeeNumber { get; set; }
    }

    public class AssignedTestTypeModel
    {
        public string TestName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
    }

    public class TechProfileIndexModel
    {
        public TechProfileModel Profile { get; set; } = new();
        public List<AssignedTestTypeModel> AssignedTestTypes { get; set; } = new();
    }
}