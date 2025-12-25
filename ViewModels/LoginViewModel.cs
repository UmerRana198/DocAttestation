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

    // Slider Captcha
    [Required(ErrorMessage = "Please complete the slider verification")]
    [Display(Name = "Slider Position")]
    public string CaptchaAnswer { get; set; } = null!;

    public string CaptchaChallengeId { get; set; } = null!;
    
    // Slider captcha images
    public string CaptchaBackgroundImage { get; set; } = null!;
    public string CaptchaPuzzleImage { get; set; } = null!;
    public int CaptchaPuzzleY { get; set; }
    
    // Legacy (not used anymore)
    public string? CaptchaQuestion { get; set; }
}
