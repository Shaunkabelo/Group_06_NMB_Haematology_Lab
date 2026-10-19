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

		public string Name { get; set; }

		public string Surname { get; set; }

		public string SaIdNumber { get; set; }

		public DateTime? DateOfBirth { get; set; }

		public string Cellphone { get; set; }

		public string Address { get; set; }

		public string Email { get; set; }

		public string? KnownMedicalConditions { get; set; }

		public string? Allergies { get; set; }

		public string? Medication { get; set; }
	}
}
