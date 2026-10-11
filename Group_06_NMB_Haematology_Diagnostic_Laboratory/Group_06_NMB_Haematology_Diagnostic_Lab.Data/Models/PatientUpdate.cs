using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models
{
    public partial class PatientUpdate
    {
        public int PatientId { get; set; }

        public int UserId { get; set; }

        [RegularExpression(@"[\p{L}][\p{L}\p{M} .\u2019\u0027-]*", ErrorMessage = "Use letters, spaces, apostrophes, periods, or hyphens only.")]
        [Required]
        public string Name { get; set; } = string.Empty;

        [RegularExpression(@"[\p{L}][\p{L}\p{M} .\u2019\u0027-]*", ErrorMessage = "Use letters, spaces, apostrophes, periods, or hyphens only.")]
        [Required]
        public string Surname { get; set; } = string.Empty;

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [RegularExpression(@"[0-9]{10}", ErrorMessage = "Cellphone number must contain exactly 10 digits.")]
        [Required]
        public string Cellphone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [RegularExpression(@"[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+", ErrorMessage = "Please enter a valid email address, for example name@example.com.")]
        public string Email { get; set; } = string.Empty;

        public string HomeAddress { get; set; } = string.Empty;
    }
}

