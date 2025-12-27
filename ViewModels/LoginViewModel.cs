using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace DocAttestation.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "CNIC or Email is required")]
    [Display(Name = "CNIC or Email")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    // Alphanumeric Captcha
    [Required(ErrorMessage = "Please enter the captcha code")]
    [StringLength(5, MinimumLength = 5, ErrorMessage = "Please enter all 5 characters")]
    [Display(Name = "Captcha Code")]
    public string CaptchaAnswer { get; set; } = string.Empty;

    // These are not user input - exclude from validation
    [ValidateNever]
    public string CaptchaChallengeId { get; set; } = string.Empty;
    
    [ValidateNever]
    public string? CaptchaImageUrl { get; set; }
}
