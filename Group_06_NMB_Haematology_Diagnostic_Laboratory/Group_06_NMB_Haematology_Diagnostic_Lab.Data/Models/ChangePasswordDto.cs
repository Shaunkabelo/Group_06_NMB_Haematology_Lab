using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models
{
	public class ChangePasswordDto
	{
		public string CurrentPassword { get; set; } = string.Empty;

		public string NewPassword { get; set; } = string.Empty;

		public string ConfirmPassword { get; set; } = string.Empty;
	}
}
