using System.ComponentModel.DataAnnotations;

namespace DocAttestation.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "CNIC is required")]
    [StringLength(13, MinimumLength = 13, ErrorMessage = "CNIC must be exactly 13 digits")]
    [RegularExpression(@"^\d{13}$", ErrorMessage = "CNIC must contain only digits")]
    [Display(Name = "CNIC")]
    public string CNIC { get; set; } = null!;

    [Required(ErrorMessage = "Mobile number is required")]
    [Phone(ErrorMessage = "Invalid mobile number")]
    [Display(Name = "Mobile Number")]
    public string MobileNumber { get; set; } = null!;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, ErrorMessage = "Password must be at least {2} characters long", MinimumLength = 8)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = null!;

    [DataType(DataType.Password)]
    [Display(Name = "Confirm Password")]
    [Compare("Password", ErrorMessage = "Password and confirmation password do not match")]
    public string ConfirmPassword { get; set; } = null!;

    [Required(ErrorMessage = "CAPTCHA answer is required")]
    [Display(Name = "CAPTCHA Answer")]
    public string CaptchaAnswer { get; set; } = null!;

    public string CaptchaChallengeId { get; set; } = null!;
    public string CaptchaQuestion { get; set; } = null!;
}

