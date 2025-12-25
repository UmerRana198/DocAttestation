using System.Security.Cryptography;
using System.Collections.Concurrent;

namespace DocAttestation.Services;

public class CaptchaService : ICaptchaService
{
    private readonly ConcurrentDictionary<string, CaptchaChallenge> _challenges = new();
    private readonly ILogger<CaptchaService> _logger;
    private readonly Timer _cleanupTimer;

    private const string Chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // No confusing chars (0,O,1,I)
    private const int CodeLength = 5;
    private const int ImageWidth = 200;
    private const int ImageHeight = 60;

    public CaptchaService(ILogger<CaptchaService> logger)
    {
        _logger = logger;
        _cleanupTimer = new Timer(CleanupExpiredChallenges, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public CaptchaChallenge GenerateChallenge()
    {
        var challengeId = Guid.NewGuid().ToString();
        var code = GenerateRandomCode();
        
        var challenge = new CaptchaChallenge
        {
            ChallengeId = challengeId,
            Question = GenerateCaptchaImage(code),
            Answer = code.GetHashCode() // Store hash, not actual code
        };

        // Store with actual code for validation
        _challenges[challengeId] = new CaptchaChallenge
        {
            ChallengeId = challengeId,
            Question = code, // Store actual code
            Answer = 0,
            CreatedAt = DateTime.UtcNow
        };

        _logger.LogInformation("Generated captcha {Id}: {Code}", challengeId, code);
        
        return challenge;
    }

    private string GenerateRandomCode()
    {
        var code = new char[CodeLength];
        for (int i = 0; i < CodeLength; i++)
        {
            code[i] = Chars[RandomNumberGenerator.GetInt32(0, Chars.Length)];
        }
        return new string(code);
    }

    private string GenerateCaptchaImage(string code)
    {
        var (bg1, bg2) = GetRandomGradient();
        var noiseLines = GenerateNoiseLines();
        var noiseDots = GenerateNoiseDots();
        var characters = GenerateStylizedCharacters(code);
        
        var svg = $@"<svg xmlns='http://www.w3.org/2000/svg' width='{ImageWidth}' height='{ImageHeight}' viewBox='0 0 {ImageWidth} {ImageHeight}'>
  <defs>
    <linearGradient id='bg' x1='0%' y1='0%' x2='100%' y2='100%'>
      <stop offset='0%' stop-color='{bg1}'/>
      <stop offset='100%' stop-color='{bg2}'/>
    </linearGradient>
    <filter id='noise'>
      <feTurbulence type='fractalNoise' baseFrequency='0.9' numOctaves='3' result='noise'/>
      <feDisplacementMap in='SourceGraphic' in2='noise' scale='2' xChannelSelector='R' yChannelSelector='G'/>
    </filter>
  </defs>
  
  <!-- Background -->
  <rect width='{ImageWidth}' height='{ImageHeight}' fill='url(#bg)' rx='8'/>
  
  <!-- Noise dots -->
  {noiseDots}
  
  <!-- Noise lines -->
  {noiseLines}
  
  <!-- Characters -->
  {characters}
  
  <!-- Scratches -->
  <line x1='10' y1='{RandomNumberGenerator.GetInt32(20, 40)}' x2='{ImageWidth - 10}' y2='{RandomNumberGenerator.GetInt32(20, 40)}' stroke='rgba(255,255,255,0.3)' stroke-width='1'/>
  <line x1='20' y1='{RandomNumberGenerator.GetInt32(25, 45)}' x2='{ImageWidth - 20}' y2='{RandomNumberGenerator.GetInt32(25, 45)}' stroke='rgba(0,0,0,0.2)' stroke-width='1'/>
</svg>";

        return ToDataUri(svg);
    }

    private string GenerateStylizedCharacters(string code)
    {
        var chars = new System.Text.StringBuilder();
        var startX = 20;
        var spacing = 35;
        
        for (int i = 0; i < code.Length; i++)
        {
            var x = startX + (i * spacing);
            var y = 38 + RandomNumberGenerator.GetInt32(-5, 6);
            var rotation = RandomNumberGenerator.GetInt32(-15, 16);
            var fontSize = 28 + RandomNumberGenerator.GetInt32(-3, 4);
            var color = GetRandomTextColor();
            
            // Shadow
            chars.Append($@"<text x='{x + 2}' y='{y + 2}' font-family='Arial Black, Arial, sans-serif' font-size='{fontSize}' font-weight='bold' fill='rgba(0,0,0,0.3)' transform='rotate({rotation},{x},{y})'>{code[i]}</text>");
            
            // Main character
            chars.Append($@"<text x='{x}' y='{y}' font-family='Arial Black, Arial, sans-serif' font-size='{fontSize}' font-weight='bold' fill='{color}' transform='rotate({rotation},{x},{y})'>{code[i]}</text>");
            
            // Highlight
            chars.Append($@"<text x='{x - 1}' y='{y - 1}' font-family='Arial Black, Arial, sans-serif' font-size='{fontSize}' font-weight='bold' fill='rgba(255,255,255,0.4)' transform='rotate({rotation},{x},{y})'>{code[i]}</text>");
        }
        
        return chars.ToString();
    }

    private string GenerateNoiseLines()
    {
        var lines = new System.Text.StringBuilder();
        var numLines = RandomNumberGenerator.GetInt32(3, 6);
        
        for (int i = 0; i < numLines; i++)
        {
            var x1 = RandomNumberGenerator.GetInt32(0, ImageWidth);
            var y1 = RandomNumberGenerator.GetInt32(0, ImageHeight);
            var x2 = RandomNumberGenerator.GetInt32(0, ImageWidth);
            var y2 = RandomNumberGenerator.GetInt32(0, ImageHeight);
            var opacity = 0.2 + (RandomNumberGenerator.GetInt32(0, 30) / 100.0);
            var color = RandomNumberGenerator.GetInt32(0, 2) == 0 ? "255,255,255" : "0,0,0";
            
            lines.Append($@"<line x1='{x1}' y1='{y1}' x2='{x2}' y2='{y2}' stroke='rgba({color},{opacity:F2})' stroke-width='{RandomNumberGenerator.GetInt32(1, 3)}'/>
");
        }
        
        return lines.ToString();
    }

    private string GenerateNoiseDots()
    {
        var dots = new System.Text.StringBuilder();
        var numDots = RandomNumberGenerator.GetInt32(30, 50);
        
        for (int i = 0; i < numDots; i++)
        {
            var cx = RandomNumberGenerator.GetInt32(5, ImageWidth - 5);
            var cy = RandomNumberGenerator.GetInt32(5, ImageHeight - 5);
            var r = RandomNumberGenerator.GetInt32(1, 4);
            var opacity = 0.1 + (RandomNumberGenerator.GetInt32(0, 20) / 100.0);
            var color = RandomNumberGenerator.GetInt32(0, 2) == 0 ? "255,255,255" : "0,0,0";
            
            dots.Append($@"<circle cx='{cx}' cy='{cy}' r='{r}' fill='rgba({color},{opacity:F2})'/>
");
        }
        
        return dots.ToString();
    }

    private (string, string) GetRandomGradient()
    {
        var gradients = new[]
        {
            ("#1a1a2e", "#16213e"), // Dark Blue
            ("#0f3460", "#16213e"), // Navy
            ("#2d4059", "#222831"), // Dark Slate
            ("#1b262c", "#0f4c75"), // Deep Ocean
            ("#212121", "#424242"), // Charcoal
            ("#1a1a1d", "#4e4e50"), // Dark Gray
            ("#2c3333", "#395b64"), // Teal Dark
            ("#1f1f1f", "#2d2d2d"), // Black
        };
        return gradients[RandomNumberGenerator.GetInt32(0, gradients.Length)];
    }

    private string GetRandomTextColor()
    {
        var colors = new[]
        {
            "#ffffff", // White
            "#f8f9fa", // Light Gray
            "#e9ecef", // Silver
            "#ffd93d", // Gold
            "#6bcb77", // Green
            "#4d96ff", // Blue
            "#ff6b6b", // Coral
            "#c9b1ff", // Lavender
        };
        return colors[RandomNumberGenerator.GetInt32(0, colors.Length)];
    }

    private static string ToDataUri(string svg)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(svg);
        return "data:image/svg+xml;base64," + Convert.ToBase64String(bytes);
    }

    public bool ValidateChallenge(string answer, string challengeId)
    {
        if (string.IsNullOrEmpty(answer) || string.IsNullOrEmpty(challengeId))
        {
            _logger.LogWarning("Empty answer or challenge ID");
            return false;
        }

        if (!_challenges.TryRemove(challengeId, out var challenge))
        {
            _logger.LogWarning("Challenge not found: {Id}", challengeId);
            return false;
        }

        // Check expiry (2 minutes)
        if ((DateTime.UtcNow - challenge.CreatedAt).TotalMinutes > 2)
        {
            _logger.LogWarning("Challenge expired: {Id}", challengeId);
            return false;
        }

        // Case-insensitive comparison
        var isValid = string.Equals(answer.Trim(), challenge.Question, StringComparison.OrdinalIgnoreCase);
        
        _logger.LogInformation("Validate captcha {Id}: expected={E}, got={G}, valid={V}", 
            challengeId, challenge.Question, answer, isValid);

        return isValid;
    }

    // Legacy methods for slider - redirect to alphanumeric
    public SliderCaptchaChallenge GenerateSliderChallenge()
    {
        var challenge = GenerateChallenge();
        return new SliderCaptchaChallenge
        {
            ChallengeId = challenge.ChallengeId,
            BackgroundImage = challenge.Question,
            PuzzlePieceImage = "",
            PuzzleY = 0,
            TargetX = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool ValidateSliderChallenge(string challengeId, int userPosition, int tolerance = 10)
    {
        return ValidateChallenge(userPosition.ToString(), challengeId);
    }

    private void CleanupExpiredChallenges(object? state)
    {
        var expired = _challenges
            .Where(x => (DateTime.UtcNow - x.Value.CreatedAt).TotalMinutes > 5)
            .Select(x => x.Key).ToList();

        foreach (var key in expired)
            _challenges.TryRemove(key, out _);

        if (expired.Count > 0)
            _logger.LogInformation("Cleaned {Count} expired captchas", expired.Count);
    }
}
