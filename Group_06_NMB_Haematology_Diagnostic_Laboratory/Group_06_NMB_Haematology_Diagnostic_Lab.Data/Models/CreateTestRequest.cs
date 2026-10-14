using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models
{
	
		public class CreateTestRequestModel
		{
			public int PatientId { get; set; }

			public string Urgency { get; set; }

			public string ClinicalNotes { get; set; }

			public List<CreateTestRequestItem> Tests { get; set; } = new();
		}
	
}
