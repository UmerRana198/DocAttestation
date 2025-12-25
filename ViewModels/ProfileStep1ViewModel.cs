using System.ComponentModel.DataAnnotations;

namespace DocAttestation.ViewModels;

public class ProfileStep1ViewModel
{
    [Required(ErrorMessage = "Full Name is required")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = null!;

    [Required(ErrorMessage = "Father Name is required")]
    [Display(Name = "Father Name")]
    public string FatherName { get; set; } = null!;

    [Required(ErrorMessage = "Date of Birth is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Date of Birth")]
    public DateTime DateOfBirth { get; set; }

    [Required(ErrorMessage = "Gender is required")]
    [Display(Name = "Gender")]
    public string Gender { get; set; } = null!;

    [Display(Name = "CNIC")]
    public string CNIC { get; set; } = null!; // Read-only, masked

    [Display(Name = "Photograph")]
    public IFormFile? Photograph { get; set; }
}

