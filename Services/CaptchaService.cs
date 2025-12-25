using System.Security.Cryptography;
using System.Collections.Concurrent;

namespace DocAttestation.Services;

public class CaptchaService : ICaptchaService
{
    private readonly ConcurrentDictionary<string, SliderCaptchaChallenge> _sliderChallenges = new();
    private readonly ILogger<CaptchaService> _logger;
    private readonly Timer _cleanupTimer;

    // Fixed dimensions
    private const int ImgWidth = 300;
    private const int ImgHeight = 150;
    private const int PieceWidth = 50;
    private const int PieceHeight = 50;

    public CaptchaService(ILogger<CaptchaService> logger)
    {
        _logger = logger;
        _cleanupTimer = new Timer(CleanupExpiredChallenges, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public SliderCaptchaChallenge GenerateSliderChallenge()
    {
        var challengeId = Guid.NewGuid().ToString();
        
        // Random target position (where the hole is)
        var targetX = RandomNumberGenerator.GetInt32(120, 230);
        var pieceY = RandomNumberGenerator.GetInt32(30, 70);
        
        // Random gradient
        var (c1, c2) = GetRandomGradient();
        
        // Generate images
        var background = CreateBackground(c1, c2, targetX, pieceY);
        var piece = CreatePiece(c1, c2);
        
        var challenge = new SliderCaptchaChallenge
        {
            ChallengeId = challengeId,
            BackgroundImage = ToDataUri(background),
            PuzzlePieceImage = ToDataUri(piece),
            PuzzleY = pieceY,
            TargetX = targetX,
            CreatedAt = DateTime.UtcNow
        };

        _sliderChallenges[challengeId] = challenge;
        _logger.LogInformation("Captcha {Id}: Target X = {X}", challengeId, targetX);
        
        return challenge;
    }

    private string CreateBackground(string c1, string c2, int holeX, int holeY)
    {
        // Simple square hole - matches the piece exactly
        return $@"<svg xmlns='http://www.w3.org/2000/svg' width='{ImgWidth}' height='{ImgHeight}'>
  <defs>
    <linearGradient id='g' x1='0%' y1='0%' x2='100%' y2='100%'>
      <stop offset='0%' stop-color='{c1}'/>
      <stop offset='100%' stop-color='{c2}'/>
    </linearGradient>
  </defs>
  <rect width='{ImgWidth}' height='{ImgHeight}' fill='url(#g)'/>
  <circle cx='60' cy='40' r='25' fill='rgba(255,255,255,0.15)'/>
  <circle cx='240' cy='110' r='35' fill='rgba(255,255,255,0.1)'/>
  <circle cx='150' cy='130' r='20' fill='rgba(255,255,255,0.12)'/>
  <rect x='{holeX}' y='{holeY}' width='{PieceWidth}' height='{PieceHeight}' rx='6' fill='rgba(0,0,0,0.45)'/>
  <rect x='{holeX}' y='{holeY}' width='{PieceWidth}' height='{PieceHeight}' rx='6' fill='none' stroke='rgba(255,255,255,0.7)' stroke-width='2'/>
</svg>";
    }

    private string CreatePiece(string c1, string c2)
    {
        // Simple rounded square that matches the hole
        return $@"<svg xmlns='http://www.w3.org/2000/svg' width='{PieceWidth + 4}' height='{PieceHeight + 4}'>
  <defs>
    <linearGradient id='pg' x1='0%' y1='0%' x2='100%' y2='100%'>
      <stop offset='0%' stop-color='{c1}'/>
      <stop offset='100%' stop-color='{c2}'/>
    </linearGradient>
    <filter id='shadow'>
      <feDropShadow dx='1' dy='1' stdDeviation='2' flood-opacity='0.4'/>
    </filter>
  </defs>
  <rect x='2' y='2' width='{PieceWidth}' height='{PieceHeight}' rx='6' fill='url(#pg)' filter='url(#shadow)'/>
  <rect x='2' y='2' width='{PieceWidth}' height='{PieceHeight}' rx='6' fill='none' stroke='white' stroke-width='2'/>
  <circle cx='27' cy='27' r='12' fill='rgba(255,255,255,0.2)'/>
</svg>";
    }

    private (string, string) GetRandomGradient()
    {
        var gradients = new[]
        {
            ("#6366f1", "#8b5cf6"), // Indigo-Purple
            ("#10b981", "#059669"), // Emerald
            ("#f59e0b", "#d97706"), // Amber
            ("#ef4444", "#dc2626"), // Red
            ("#3b82f6", "#2563eb"), // Blue
            ("#ec4899", "#db2777"), // Pink
            ("#14b8a6", "#0d9488"), // Teal
            ("#8b5cf6", "#7c3aed"), // Violet
            ("#f97316", "#ea580c"), // Orange
            ("#06b6d4", "#0891b2"), // Cyan
        };
        return gradients[RandomNumberGenerator.GetInt32(0, gradients.Length)];
    }

    private static string ToDataUri(string svg)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(svg);
        return "data:image/svg+xml;base64," + Convert.ToBase64String(bytes);
    }

    public bool ValidateSliderChallenge(string challengeId, int userPosition, int tolerance = 10)
    {
        if (string.IsNullOrEmpty(challengeId))
        {
            _logger.LogWarning("Empty challenge ID");
            return false;
        }

        if (!_sliderChallenges.TryRemove(challengeId, out var challenge))
        {
            _logger.LogWarning("Challenge not found: {Id}", challengeId);
            return false;
        }

        if ((DateTime.UtcNow - challenge.CreatedAt).TotalMinutes > 2)
        {
            _logger.LogWarning("Challenge expired: {Id}", challengeId);
            return false;
        }

        var diff = Math.Abs(userPosition - challenge.TargetX);
        var valid = diff <= tolerance;
        
        _logger.LogInformation("Validate: target={T}, user={U}, diff={D}, valid={V}", 
            challenge.TargetX, userPosition, diff, valid);

        return valid;
    }

    public CaptchaChallenge GenerateChallenge()
    {
        var slider = GenerateSliderChallenge();
        return new CaptchaChallenge
        {
            ChallengeId = slider.ChallengeId,
            Question = "slider",
            Answer = slider.TargetX
        };
    }

    public bool ValidateChallenge(string answer, string challengeId)
    {
        if (string.IsNullOrEmpty(answer) || string.IsNullOrEmpty(challengeId))
            return false;
        if (!int.TryParse(answer, out var pos))
            return false;
        return ValidateSliderChallenge(challengeId, pos);
    }

    private void CleanupExpiredChallenges(object? state)
    {
        var expired = _sliderChallenges
            .Where(x => (DateTime.UtcNow - x.Value.CreatedAt).TotalMinutes > 5)
            .Select(x => x.Key).ToList();

        foreach (var key in expired)
            _sliderChallenges.TryRemove(key, out _);

        if (expired.Count > 0)
            _logger.LogInformation("Cleaned {Count} expired captchas", expired.Count);
    }
}
