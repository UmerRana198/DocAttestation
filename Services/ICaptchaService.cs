namespace DocAttestation.Services;

public interface ICaptchaService
{
    /// <summary>
    /// Generate a slider captcha challenge
    /// </summary>
    SliderCaptchaChallenge GenerateSliderChallenge();
    
    /// <summary>
    /// Validate the slider captcha
    /// </summary>
    bool ValidateSliderChallenge(string challengeId, int userPosition, int tolerance = 5);
    
    /// <summary>
    /// Legacy: Generate old math-based captcha (for backward compatibility)
    /// </summary>
    CaptchaChallenge GenerateChallenge();
    
    /// <summary>
    /// Legacy: Validate old math-based captcha (for backward compatibility)
    /// </summary>
    bool ValidateChallenge(string answer, string challengeId);
}

public class SliderCaptchaChallenge
{
    public string ChallengeId { get; set; } = null!;
    
    /// <summary>
    /// Background image with cut-out (Base64)
    /// </summary>
    public string BackgroundImage { get; set; } = null!;
    
    /// <summary>
    /// Puzzle piece image (Base64)
    /// </summary>
    public string PuzzlePieceImage { get; set; } = null!;
    
    /// <summary>
    /// Y position of puzzle (for display)
    /// </summary>
    public int PuzzleY { get; set; }
    
    /// <summary>
    /// Target X position (server-side only, not sent to client)
    /// </summary>
    public int TargetX { get; set; }
    
    /// <summary>
    /// Creation time for expiry check
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CaptchaChallenge
{
    public string ChallengeId { get; set; } = null!;
    public string Question { get; set; } = null!;
    public int Answer { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
