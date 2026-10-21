using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models
{
	public class DoctorResultRowDto
	{
        public string UnitOfMeasurement { get; set; } = "";
        public decimal? NormalRangeMin { get; set; }
        public decimal? NormalRangeMax { get; set; }
		public int TestRequestId { get; set; }

		public DateTime RequestDate { get; set; }

		public string RequestStatus { get; set; } = "";

		public string Urgency { get; set; } = "";

		public DateTime? ReleaseDate { get; set; }

		public string PatientName { get; set; } = "";

		public int PatientId { get; set; }

		public string PatientSaId { get; set; } = "";

		public string TestName { get; set; } = "";

		public int LabTestId { get; set; }

		public int TestResultId { get; set; }

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
