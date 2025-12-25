namespace DocAttestation.Services;

public interface ICaptchaService
{
    CaptchaChallenge GenerateChallenge();
    bool ValidateChallenge(string answer, string challengeId);
}

public class CaptchaChallenge
{
    public string ChallengeId { get; set; } = null!;
    public string Question { get; set; } = null!;
    public int Answer { get; set; }
}

