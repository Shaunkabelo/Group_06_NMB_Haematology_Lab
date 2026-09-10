using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models
{
	public class CreateTestRequestItem
	{
		public int TestTypeId { get; set; }

		public int SampleTypeId { get; set; }

		public string BarcodeNumber { get; set; }
	}
}
