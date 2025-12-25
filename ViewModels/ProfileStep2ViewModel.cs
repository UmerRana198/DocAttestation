using System.ComponentModel.DataAnnotations;

namespace DocAttestation.ViewModels;

public class ProfileStep2ViewModel
{
    [Required(ErrorMessage = "Mobile Number is required")]
    [Phone(ErrorMessage = "Invalid mobile number")]
    [Display(Name = "Mobile Number")]
    public string MobileNumber { get; set; } = null!;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Address is required")]
    [Display(Name = "Address")]
    public string Address { get; set; } = null!;
}

