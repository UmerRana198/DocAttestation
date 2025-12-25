using System.Security.Cryptography;
using System.Collections.Concurrent;

namespace DocAttestation.Services;

public class CaptchaService : ICaptchaService
{
    private readonly ConcurrentDictionary<string, SliderCaptchaChallenge> _sliderChallenges = new();
    private readonly ILogger<CaptchaService> _logger;
    private readonly Timer _cleanupTimer;

    // Pre-defined background patterns (SVG-based for better quality)
    private readonly string[] _backgroundPatterns = new[]
    {
        // Pattern 1: Geometric
        @"<svg xmlns='http://www.w3.org/2000/svg' width='300' height='150'>
            <defs>
                <linearGradient id='bg1' x1='0%' y1='0%' x2='100%' y2='100%'>
                    <stop offset='0%' style='stop-color:#667eea;stop-opacity:1' />
                    <stop offset='100%' style='stop-color:#764ba2;stop-opacity:1' />
                </linearGradient>
            </defs>
            <rect width='300' height='150' fill='url(#bg1)'/>
            <circle cx='50' cy='30' r='20' fill='rgba(255,255,255,0.2)'/>
            <circle cx='250' cy='120' r='35' fill='rgba(255,255,255,0.15)'/>
            <rect x='120' y='60' width='40' height='40' rx='5' fill='rgba(255,255,255,0.1)' transform='rotate(15,140,80)'/>
            <polygon points='200,20 220,50 180,50' fill='rgba(255,255,255,0.2)'/>
        </svg>",
        
        // Pattern 2: Nature
        @"<svg xmlns='http://www.w3.org/2000/svg' width='300' height='150'>
            <defs>
                <linearGradient id='bg2' x1='0%' y1='0%' x2='0%' y2='100%'>
                    <stop offset='0%' style='stop-color:#11998e;stop-opacity:1' />
                    <stop offset='100%' style='stop-color:#38ef7d;stop-opacity:1' />
                </linearGradient>
            </defs>
            <rect width='300' height='150' fill='url(#bg2)'/>
            <ellipse cx='60' cy='130' rx='50' ry='20' fill='rgba(255,255,255,0.2)'/>
            <ellipse cx='200' cy='135' rx='70' ry='15' fill='rgba(255,255,255,0.15)'/>
            <path d='M150,80 Q170,40 190,80 T230,80' stroke='rgba(255,255,255,0.3)' fill='none' stroke-width='3'/>
        </svg>",
        
        // Pattern 3: Tech
        @"<svg xmlns='http://www.w3.org/2000/svg' width='300' height='150'>
            <defs>
                <linearGradient id='bg3' x1='0%' y1='0%' x2='100%' y2='0%'>
                    <stop offset='0%' style='stop-color:#0f2027;stop-opacity:1' />
                    <stop offset='50%' style='stop-color:#203a43;stop-opacity:1' />
                    <stop offset='100%' style='stop-color:#2c5364;stop-opacity:1' />
                </linearGradient>
            </defs>
            <rect width='300' height='150' fill='url(#bg3)'/>
            <line x1='0' y1='30' x2='300' y2='30' stroke='rgba(255,255,255,0.1)' stroke-width='1'/>
            <line x1='0' y1='75' x2='300' y2='75' stroke='rgba(255,255,255,0.1)' stroke-width='1'/>
            <line x1='0' y1='120' x2='300' y2='120' stroke='rgba(255,255,255,0.1)' stroke-width='1'/>
            <circle cx='80' cy='75' r='25' stroke='rgba(255,255,255,0.2)' fill='none' stroke-width='2'/>
            <circle cx='220' cy='75' r='30' stroke='rgba(255,255,255,0.15)' fill='none' stroke-width='2'/>
        </svg>",
        
        // Pattern 4: Warm
        @"<svg xmlns='http://www.w3.org/2000/svg' width='300' height='150'>
            <defs>
                <linearGradient id='bg4' x1='0%' y1='0%' x2='100%' y2='100%'>
                    <stop offset='0%' style='stop-color:#f093fb;stop-opacity:1' />
                    <stop offset='100%' style='stop-color:#f5576c;stop-opacity:1' />
                </linearGradient>
            </defs>
            <rect width='300' height='150' fill='url(#bg4)'/>
            <circle cx='30' cy='120' r='60' fill='rgba(255,255,255,0.1)'/>
            <circle cx='270' cy='30' r='50' fill='rgba(255,255,255,0.15)'/>
            <rect x='100' y='50' width='100' height='50' rx='10' fill='rgba(255,255,255,0.1)'/>
        </svg>",
        
        // Pattern 5: Ocean
        @"<svg xmlns='http://www.w3.org/2000/svg' width='300' height='150'>
            <defs>
                <linearGradient id='bg5' x1='0%' y1='0%' x2='0%' y2='100%'>
                    <stop offset='0%' style='stop-color:#4facfe;stop-opacity:1' />
                    <stop offset='100%' style='stop-color:#00f2fe;stop-opacity:1' />
                </linearGradient>
            </defs>
            <rect width='300' height='150' fill='url(#bg5)'/>
            <path d='M0,100 Q75,80 150,100 T300,100 L300,150 L0,150 Z' fill='rgba(255,255,255,0.2)'/>
            <path d='M0,120 Q75,100 150,120 T300,120 L300,150 L0,150 Z' fill='rgba(255,255,255,0.15)'/>
        </svg>"
    };

    public CaptchaService(ILogger<CaptchaService> logger)
    {
        _logger = logger;
        // Clean up expired challenges every 5 minutes
        _cleanupTimer = new Timer(CleanupExpiredChallenges, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public SliderCaptchaChallenge GenerateSliderChallenge()
    {
        var challengeId = Guid.NewGuid().ToString();
        
        // Generate random position for puzzle (X: 80-220, Y: 30-90)
        var targetX = RandomNumberGenerator.GetInt32(80, 220);
        var puzzleY = RandomNumberGenerator.GetInt32(30, 90);
        
        // Select random background
        var bgIndex = RandomNumberGenerator.GetInt32(0, _backgroundPatterns.Length);
        var backgroundSvg = _backgroundPatterns[bgIndex];
        
        // Add puzzle cutout to background
        var backgroundWithCutout = AddPuzzleCutout(backgroundSvg, targetX, puzzleY);
        
        // Generate puzzle piece
        var puzzlePieceSvg = GeneratePuzzlePiece(backgroundSvg, targetX, puzzleY);
        
        var challenge = new SliderCaptchaChallenge
        {
            ChallengeId = challengeId,
            BackgroundImage = "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(backgroundWithCutout)),
            PuzzlePieceImage = "data:image/svg+xml;base64," + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(puzzlePieceSvg)),
            PuzzleY = puzzleY,
            TargetX = targetX,
            CreatedAt = DateTime.UtcNow
        };

        _sliderChallenges[challengeId] = challenge;
        
        return challenge;
    }

    public bool ValidateSliderChallenge(string challengeId, int userPosition, int tolerance = 5)
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

        if (!isValid)
        {
            _logger.LogWarning("Slider validation failed. Expected: {Expected}, Got: {Got}, Diff: {Diff}", 
                challenge.TargetX, userPosition, diff);
        }

        return isValid;
    }

    // Legacy method - redirect to slider captcha
    public CaptchaChallenge GenerateChallenge()
    {
        var slider = GenerateSliderChallenge();
        return new CaptchaChallenge
        {
            ChallengeId = slider.ChallengeId,
            Question = "slider", // Marker for slider captcha
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

    private string AddPuzzleCutout(string svg, int x, int y)
    {
        // Insert the puzzle cutout before closing </svg>
        var puzzlePath = GetPuzzlePath(x, y);
        var cutoutSvg = $@"<defs>
            <mask id='puzzleMask'>
                <rect width='300' height='150' fill='white'/>
                <path d='{puzzlePath}' fill='black'/>
            </mask>
        </defs>
        <rect width='300' height='150' fill='rgba(0,0,0,0.4)' mask='url(#puzzleMask)'/>
        <path d='{puzzlePath}' fill='none' stroke='rgba(255,255,255,0.5)' stroke-width='2'/>
        ";
        
        return svg.Replace("</svg>", cutoutSvg + "</svg>");
    }

    private string GeneratePuzzlePiece(string backgroundSvg, int x, int y)
    {
        var puzzlePath = GetPuzzlePath(0, 0); // Piece at origin
        
        // Create puzzle piece with gradient from background
        return $@"<svg xmlns='http://www.w3.org/2000/svg' width='50' height='50' viewBox='0 0 50 50'>
            <defs>
                <linearGradient id='pieceBg' x1='0%' y1='0%' x2='100%' y2='100%'>
                    <stop offset='0%' style='stop-color:#667eea;stop-opacity:1' />
                    <stop offset='100%' style='stop-color:#764ba2;stop-opacity:1' />
                </linearGradient>
                <filter id='shadow' x='-50%' y='-50%' width='200%' height='200%'>
                    <feDropShadow dx='2' dy='2' stdDeviation='3' flood-opacity='0.5'/>
                </filter>
            </defs>
            <path d='{puzzlePath}' fill='url(#pieceBg)' stroke='white' stroke-width='2' filter='url(#shadow)'/>
        </svg>";
    }

    private string GetPuzzlePath(int offsetX, int offsetY)
    {
        // Puzzle piece shape (jigsaw-like)
        var x = offsetX;
        var y = offsetY;
        return $@"M{x},{y} 
                  l10,0 
                  c0,0 0,-5 5,-5 
                  c5,0 5,5 5,5 
                  l10,0 
                  l0,10 
                  c0,0 5,0 5,5 
                  c0,5 -5,5 -5,5 
                  l0,10 
                  l-10,0 
                  c0,0 0,5 -5,5 
                  c-5,0 -5,-5 -5,-5 
                  l-10,0 
                  l0,-10 
                  c0,0 -5,0 -5,-5 
                  c0,-5 5,-5 5,-5 
                  l0,-10 Z";
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
