using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models
{
	public class TestTypeDto
	{
		public int TestTypeId { get; set; }

		public int TestCategoryId { get; set; }

		public string TestName { get; set; } =
			string.Empty;

		public int SampleTypeId { get; set; }

		public string SampleTypeName { get; set; } =
			string.Empty;
	}
}
