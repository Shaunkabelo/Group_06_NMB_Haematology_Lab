using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models
{
	public class DoctorAccountDto
	{
		public int DoctorId { get; set; }

		public int UserId { get; set; }

		public string Name { get; set; } = string.Empty;

		public string Surname { get; set; } = string.Empty;

		public string Email { get; set; } = string.Empty;

		public string Cellphone { get; set; } = string.Empty;

		public string Role { get; set; } = string.Empty;

		public string HpcsaNumber { get; set; } = string.Empty;
	}
}

