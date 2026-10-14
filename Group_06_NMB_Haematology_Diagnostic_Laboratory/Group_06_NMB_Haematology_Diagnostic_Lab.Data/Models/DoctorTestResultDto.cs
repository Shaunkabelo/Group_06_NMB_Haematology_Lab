using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models
{
	public class DoctorTestResultDto
	{
		public int TestResultId { get; set; }

		public int LabTestId { get; set; }

		public decimal? ResultValue { get; set; }

		public string? ResultText { get; set; }

		public string? Notes { get; set; }

		public bool IsAbnormal { get; set; }

		public DateTime? CaptureDateTime { get; set; }

		public string? VerificationStatus { get; set; }

		public DateTime? VerificationDateTime { get; set; }

		public string? ReviewStatus { get; set; }

		public string? ReviewNotes { get; set; }
	}
}
