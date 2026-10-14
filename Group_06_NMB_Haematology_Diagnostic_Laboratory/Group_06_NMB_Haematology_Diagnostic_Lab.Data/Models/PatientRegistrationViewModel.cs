using System.ComponentModel.DataAnnotations;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;

public class PatientRegistrationViewModel
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = "";

    [Required]
    [StringLength(100)]
    public string Surname { get; set; } = "";

    [Required]
    [RegularExpression(@"^[0-9]{13}$",
        ErrorMessage = "Enter a valid 13-digit SA ID number.")]
    public string SaIdNumber { get; set; } = "";

    [Required]
    [DataType(DataType.Date)]
    public DateTime DateOfBirth { get; set; }

    [Required]
    [RegularExpression(@"^[0-9]{10}$",
        ErrorMessage = "Enter a 10-digit cellphone number.")]
    public string Cellphone { get; set; } = "";

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = "";

    [Required]
    [StringLength(250)]
    public string HomeAddress { get; set; } = "";

    [Required]
    [StringLength(128, MinimumLength = 8)]
    [RegularExpression(
        @"^(?=.*[A-Z])(?=.*[a-z])(?=.*[0-9])(?=.*[^a-zA-Z0-9]).{8,}$",
        ErrorMessage =
            "Password must contain uppercase and lowercase letters, a number and a special character.")]
    public string Password { get; set; } = "";

    [Required]
    [Compare(nameof(Password),
        ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = "";
}
