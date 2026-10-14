using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models
{
	public class DoctorAlertDto
	{
		public int TestResultId { get; set; }
		public int TestRequestId { get; set; }

		public string PatientName { get; set; } = "";
		public string TestName { get; set; } = "";
		public string Urgency { get; set; } = "";

		public DateTime? CaptureDateTime { get; set; }

		public bool IsAbnormal { get; set; }
	}
}
