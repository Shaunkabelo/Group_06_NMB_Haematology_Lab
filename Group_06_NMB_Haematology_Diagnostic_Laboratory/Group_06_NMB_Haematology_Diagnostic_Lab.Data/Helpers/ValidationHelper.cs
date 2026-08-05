using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Helpers
{
    public static class ValidationHelper
    {
        public static bool IsSouthAfricanIdMatchDob(string idNumber, DateTime? dateOfBirth)
        {
            if (string.IsNullOrWhiteSpace(idNumber))
                return false;

            if (dateOfBirth == null)
                return false;

            if (idNumber.Length != 13)
                return false;

            string idDob = idNumber.Substring(0, 6);

            string dob = dateOfBirth.Value.ToString("yyMMdd");

            return idDob == dob;
        }
    }
}
