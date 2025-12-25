using System.ComponentModel.DataAnnotations;

namespace DocAttestation.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "CNIC or Email is required")]
    [Display(Name = "CNIC or Email")]
    public string Login { get; set; } = null!;

    [Required(ErrorMessage = "Password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = null!;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    [Required(ErrorMessage = "CAPTCHA answer is required")]
    [Display(Name = "CAPTCHA Answer")]
    public string CaptchaAnswer { get; set; } = null!;

    public string CaptchaChallengeId { get; set; } = null!;
    public string CaptchaQuestion { get; set; } = null!;
}

