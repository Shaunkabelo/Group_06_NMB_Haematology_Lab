using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models
{
    using System.ComponentModel.DataAnnotations;

    public class PatientRegistrationViewModel
    {
        [Required]
        [RegularExpression(@"[\p{L}][\p{L}\p{M} .\u2019\u0027-]*", ErrorMessage = "Use letters, spaces, apostrophes, periods, or hyphens only.")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"[\p{L}][\p{L}\p{M} .\u2019\u0027-]*", ErrorMessage = "Use letters, spaces, apostrophes, periods, or hyphens only.")]
        public string Surname { get; set; } = string.Empty;

        [Required]
        [SouthAfricanId]
        public string SaIdNumber { get; set; } = string.Empty;

        [Required]
        public DateOnly? DateOfBirth { get; set; }

        [Required]
        [RegularExpression(@"[0-9]{10}", ErrorMessage = "Cellphone number must contain exactly 10 digits.")]
        public string Cellphone { get; set; } = string.Empty;

        [Required]
        [EmailAddress(ErrorMessage = "Enter a valid email address, such as patient@example.com.")]
        [RegularExpression(@"[^\s@]+@[^\s@.]+(?:\.[^\s@.]+)+", ErrorMessage = "Enter a valid email address, such as patient@example.com.")]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string HomeAddress { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
