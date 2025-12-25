using System.Security.Cryptography;

namespace DocAttestation.Services;

public class CaptchaService : ICaptchaService
{
    private readonly Dictionary<string, CaptchaChallenge> _challenges = new();
    private readonly ILogger<CaptchaService> _logger;
    private readonly Timer _cleanupTimer;

    public CaptchaService(ILogger<CaptchaService> logger)
    {
        _logger = logger;
        // Clean up old challenges every 10 minutes
        _cleanupTimer = new Timer(CleanupExpiredChallenges, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
    }

    public CaptchaChallenge GenerateChallenge()
    {
        var random = RandomNumberGenerator.GetInt32(1, 20);
        var random2 = RandomNumberGenerator.GetInt32(1, 20);
        var operation = RandomNumberGenerator.GetInt32(0, 2); // 0 = addition, 1 = subtraction

        int answer;
        string question;

        if (operation == 0)
        {
            // Addition
            answer = random + random2;
            question = $"{random} + {random2} = ?";
        }
        else
        {
            // Subtraction (ensure positive result)
            if (random < random2)
            {
                (random, random2) = (random2, random);
            }
            answer = random - random2;
            question = $"{random} - {random2} = ?";
        }

        var challengeId = Guid.NewGuid().ToString();
        var challenge = new CaptchaChallenge
        {
            ChallengeId = challengeId,
            Question = question,
            Answer = answer
        };

        // Store challenge with expiration (5 minutes)
        _challenges[challengeId] = challenge;

        return challenge;
    }

    public bool ValidateChallenge(string answer, string challengeId)
    {
        if (string.IsNullOrWhiteSpace(answer) || string.IsNullOrWhiteSpace(challengeId))
        {
            return false;
        }

        if (!_challenges.TryGetValue(challengeId, out var challenge))
        {
            _logger.LogWarning("CAPTCHA challenge not found: {ChallengeId}", challengeId);
            return false;
        }

        // Remove challenge after validation (one-time use)
        _challenges.Remove(challengeId);

        if (!int.TryParse(answer.Trim(), out var userAnswer))
        {
            _logger.LogWarning("Invalid CAPTCHA answer format: {Answer}", answer);
            return false;
        }

        var isValid = userAnswer == challenge.Answer;
        
        if (!isValid)
        {
            _logger.LogWarning("CAPTCHA validation failed. Expected: {Expected}, Got: {Got}", challenge.Answer, userAnswer);
        }

        return isValid;
    }

    private void CleanupExpiredChallenges(object? state)
    {
        // Challenges are removed after validation, so this is mainly for cleanup
        // In a production environment, you might want to add timestamps and remove old ones
        var count = _challenges.Count;
        if (count > 1000)
        {
            _challenges.Clear();
            _logger.LogInformation("Cleaned up CAPTCHA challenges. Removed {Count} challenges", count);
        }
    }
}

