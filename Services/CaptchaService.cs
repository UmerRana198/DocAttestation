using System.Security.Cryptography;
using System.Collections.Concurrent;

namespace DocAttestation.Services;

public class CaptchaService : ICaptchaService
{
    private readonly ConcurrentDictionary<string, SliderCaptchaChallenge> _sliderChallenges = new();
    private readonly ILogger<CaptchaService> _logger;
    private readonly Timer _cleanupTimer;

    // Image dimensions
    private const int ImageWidth = 300;
    private const int ImageHeight = 150;
    private const int PuzzleSize = 44; // Size of puzzle piece
    private const int PuzzleKnob = 10; // Size of knob

    public CaptchaService(ILogger<CaptchaService> logger)
    {
        _logger = logger;
        _cleanupTimer = new Timer(CleanupExpiredChallenges, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public SliderCaptchaChallenge GenerateSliderChallenge()
    {
        var challengeId = Guid.NewGuid().ToString();
        
        // Random position for puzzle target (leave room for puzzle piece)
        var targetX = RandomNumberGenerator.GetInt32(100, 220);
        var puzzleY = RandomNumberGenerator.GetInt32(25, 80);
        
        // Select random gradient colors
        var (color1, color2) = GetRandomGradient();
        
        // Generate background with cutout hole
        var backgroundSvg = GenerateBackgroundWithHole(color1, color2, targetX, puzzleY);
        
        // Generate matching puzzle piece
        var puzzlePieceSvg = GeneratePuzzlePiece(color1, color2, targetX, puzzleY);
        
        var challenge = new SliderCaptchaChallenge
        {
            ChallengeId = challengeId,
            BackgroundImage = "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(backgroundSvg)),
            PuzzlePieceImage = "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(puzzlePieceSvg)),
            PuzzleY = puzzleY,
            TargetX = targetX,
            CreatedAt = DateTime.UtcNow
        };

        _sliderChallenges[challengeId] = challenge;
        
        _logger.LogInformation("Generated slider captcha {ChallengeId} with target at X={TargetX}", challengeId, targetX);
        
        return challenge;
    }

    private (string, string) GetRandomGradient()
    {
        var gradients = new[]
        {
            ("#667eea", "#764ba2"), // Purple
            ("#11998e", "#38ef7d"), // Green
            ("#ee0979", "#ff6a00"), // Red-Orange
            ("#3494e6", "#ec6ead"), // Blue-Pink
            ("#f093fb", "#f5576c"), // Pink
            ("#4facfe", "#00f2fe"), // Cyan
            ("#43e97b", "#38f9d7"), // Mint
            ("#fa709a", "#fee140"), // Coral
            ("#a18cd1", "#fbc2eb"), // Lavender
            ("#667db6", "#0082c8"), // Blue
        };
        
        var index = RandomNumberGenerator.GetInt32(0, gradients.Length);
        return gradients[index];
    }

    private string GenerateBackgroundWithHole(string color1, string color2, int holeX, int holeY)
    {
        var patterns = GenerateRandomPatterns();
        
        return $@"<svg xmlns='http://www.w3.org/2000/svg' width='{ImageWidth}' height='{ImageHeight}' viewBox='0 0 {ImageWidth} {ImageHeight}'>
            <defs>
                <linearGradient id='bgGrad' x1='0%' y1='0%' x2='100%' y2='100%'>
                    <stop offset='0%' style='stop-color:{color1};stop-opacity:1' />
                    <stop offset='100%' style='stop-color:{color2};stop-opacity:1' />
                </linearGradient>
                <clipPath id='puzzleHole'>
                    <rect width='{ImageWidth}' height='{ImageHeight}'/>
                    <path d='{GetPuzzleShapePath(holeX, holeY)}' clip-rule='evenodd'/>
                </clipPath>
            </defs>
            
            <!-- Main background -->
            <rect width='{ImageWidth}' height='{ImageHeight}' fill='url(#bgGrad)'/>
            
            <!-- Decorative patterns -->
            {patterns}
            
            <!-- Dark overlay where puzzle piece goes (the hole) -->
            <path d='{GetPuzzleShapePath(holeX, holeY)}' fill='rgba(0,0,0,0.5)'/>
            
            <!-- Hole border/outline -->
            <path d='{GetPuzzleShapePath(holeX, holeY)}' fill='none' stroke='rgba(255,255,255,0.6)' stroke-width='2'/>
            
            <!-- Inner shadow effect for depth -->
            <path d='{GetPuzzleShapePath(holeX + 2, holeY + 2)}' fill='none' stroke='rgba(0,0,0,0.3)' stroke-width='1'/>
        </svg>";
    }

    private string GeneratePuzzlePiece(string color1, string color2, int sourceX, int sourceY)
    {
        // The puzzle piece shows the original background content that was "cut out"
        var patterns = GenerateRandomPatterns();
        var pieceWidth = PuzzleSize + PuzzleKnob + 4;
        var pieceHeight = PuzzleSize + PuzzleKnob + 4;
        
        return $@"<svg xmlns='http://www.w3.org/2000/svg' width='{pieceWidth}' height='{pieceHeight}' viewBox='0 0 {pieceWidth} {pieceHeight}'>
            <defs>
                <linearGradient id='pieceGrad' x1='0%' y1='0%' x2='100%' y2='100%'>
                    <stop offset='0%' style='stop-color:{color1};stop-opacity:1' />
                    <stop offset='100%' style='stop-color:{color2};stop-opacity:1' />
                </linearGradient>
                <clipPath id='pieceClip'>
                    <path d='{GetPuzzleShapePath(2, 2)}'/>
                </clipPath>
                <filter id='dropShadow' x='-50%' y='-50%' width='200%' height='200%'>
                    <feDropShadow dx='2' dy='2' stdDeviation='3' flood-color='rgba(0,0,0,0.4)'/>
                </filter>
            </defs>
            
            <!-- Puzzle piece with shadow -->
            <g filter='url(#dropShadow)'>
                <!-- Background fill for the piece -->
                <path d='{GetPuzzleShapePath(2, 2)}' fill='url(#pieceGrad)'/>
                
                <!-- Some pattern overlay for texture -->
                <g clip-path='url(#pieceClip)' opacity='0.3'>
                    <circle cx='25' cy='15' r='12' fill='rgba(255,255,255,0.3)'/>
                    <circle cx='35' cy='35' r='8' fill='rgba(255,255,255,0.2)'/>
                </g>
                
                <!-- Border -->
                <path d='{GetPuzzleShapePath(2, 2)}' fill='none' stroke='white' stroke-width='2'/>
            </g>
        </svg>";
    }

    private string GetPuzzleShapePath(int x, int y)
    {
        // Create a jigsaw puzzle piece shape with a knob on the right
        var s = PuzzleSize; // main square size
        var k = PuzzleKnob; // knob size
        
        // Starting from top-left, going clockwise
        // The shape is a square with a circular knob sticking out on the right side
        return $@"M{x},{y} 
                  L{x + s},{y} 
                  L{x + s},{y + s/2 - k/2} 
                  C{x + s},{y + s/2 - k/2} {x + s + k},{y + s/2 - k} {x + s + k},{y + s/2} 
                  C{x + s + k},{y + s/2 + k} {x + s},{y + s/2 + k/2} {x + s},{y + s/2 + k/2} 
                  L{x + s},{y + s} 
                  L{x},{y + s} 
                  L{x},{y + s/2 + k/2} 
                  C{x},{y + s/2 + k/2} {x - k},{y + s/2 + k} {x - k},{y + s/2} 
                  C{x - k},{y + s/2 - k} {x},{y + s/2 - k/2} {x},{y + s/2 - k/2} 
                  Z";
    }

    private string GenerateRandomPatterns()
    {
        var patterns = new System.Text.StringBuilder();
        var numPatterns = RandomNumberGenerator.GetInt32(4, 8);
        
        for (int i = 0; i < numPatterns; i++)
        {
            var cx = RandomNumberGenerator.GetInt32(20, 280);
            var cy = RandomNumberGenerator.GetInt32(20, 130);
            var r = RandomNumberGenerator.GetInt32(15, 45);
            var opacity = 0.05 + (RandomNumberGenerator.GetInt32(0, 15) / 100.0);
            
            patterns.Append($"<circle cx='{cx}' cy='{cy}' r='{r}' fill='rgba(255,255,255,{opacity:F2})'/>");
        }
        
        return patterns.ToString();
    }

    public bool ValidateSliderChallenge(string challengeId, int userPosition, int tolerance = 8)
    {
        if (string.IsNullOrEmpty(challengeId))
        {
            _logger.LogWarning("Empty challenge ID provided");
            return false;
        }

        if (!_sliderChallenges.TryRemove(challengeId, out var challenge))
        {
            _logger.LogWarning("Slider challenge not found: {ChallengeId}", challengeId);
            return false;
        }

        // Check expiry (2 minutes)
        if ((DateTime.UtcNow - challenge.CreatedAt).TotalMinutes > 2)
        {
            _logger.LogWarning("Slider challenge expired: {ChallengeId}", challengeId);
            return false;
        }

        // Check if position is within tolerance
        var diff = Math.Abs(userPosition - challenge.TargetX);
        var isValid = diff <= tolerance;

        _logger.LogInformation("Slider validation: Expected={Expected}, Got={Got}, Diff={Diff}, Valid={Valid}", 
            challenge.TargetX, userPosition, diff, isValid);

        return isValid;
    }

    // Legacy method - redirect to slider captcha
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

    // Legacy method - validate slider position passed as string
    public bool ValidateChallenge(string answer, string challengeId)
    {
        if (string.IsNullOrEmpty(answer) || string.IsNullOrEmpty(challengeId))
            return false;

        if (!int.TryParse(answer, out var position))
            return false;

        return ValidateSliderChallenge(challengeId, position);
    }

    private void CleanupExpiredChallenges(object? state)
    {
        var expiredKeys = _sliderChallenges
            .Where(kvp => (DateTime.UtcNow - kvp.Value.CreatedAt).TotalMinutes > 5)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _sliderChallenges.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired slider captcha challenges", expiredKeys.Count);
        }
    }
}
