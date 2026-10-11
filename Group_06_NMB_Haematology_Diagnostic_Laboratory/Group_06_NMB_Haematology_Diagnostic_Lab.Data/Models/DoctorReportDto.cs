using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models
{
	public class DoctorReportDto
	{
		public int TestRequestId { get; set; }

		public string PatientName { get; set; } = string.Empty;

		public string PatientSaIdNumber { get; set; } = string.Empty;

		public DateTime RequestDate { get; set; }

		public string Urgency { get; set; } = string.Empty;

		public string Status { get; set; } = string.Empty;

		public string Tests { get; set; } = string.Empty;
	}
}
