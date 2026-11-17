using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models
{
	public  class PatientDetailsDto
	{
		public int PatientId { get; set; }

		public int UserId { get; set; }

		public string Name { get; set; } = string.Empty;

		public string Surname { get; set; } = string.Empty;

		public string SaIdNumber { get; set; } = string.Empty;

		public DateTime? DateOfBirth { get; set; }

		public string Cellphone { get; set; } = string.Empty;

		public string Address { get; set; } = string.Empty;

		public string Email { get; set; } = string.Empty;

		public string? KnownMedicalConditions { get; set; }

		public string? Allergies { get; set; }

		public string? Medication { get; set; }
	}
}
