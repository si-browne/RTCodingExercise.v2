using System.Text;
using System.Text.RegularExpressions;

namespace Catalog.API.Services;

/// <summary>
/// Advanced plate registration matching service using phonetic and pattern-based algorithms.
/// Implements industry-standard approaches for license plate name matching.
/// </summary>
public class PlateMatchingService : IPlateMatchingService
{
    // Number-to-letter phonetic mappings commonly used in license plates
    private static readonly Dictionary<char, char[]> NumberToLetterMap = new()
    {
        { '0', new[] { 'O', 'Q' } },
        { '1', new[] { 'I', 'L' } },
        { '3', new[] { 'E', 'B' } },
        { '4', new[] { 'A' } },
        { '5', new[] { 'S' } },
        { '6', new[] { 'G' } },
        { '7', new[] { 'T', 'L' } },
        { '8', new[] { 'B' } },
        { '9', new[] { 'G' } }
    };

    // Letter-to-number phonetic mappings (reverse of above)
    private static readonly Dictionary<char, char[]> LetterToNumberMap = new()
    {
        { 'O', new[] { '0' } },
        { 'Q', new[] { '0' } },
        { 'I', new[] { '1' } },
        { 'L', new[] { '1', '7' } },
        { 'E', new[] { '3' } },
        { 'B', new[] { '3', '8' } },
        { 'A', new[] { '4' } },
        { 'S', new[] { '5' } },
        { 'G', new[] { '6', '9' } },
        { 'T', new[] { '7' } }
    };

    private readonly ILogger<PlateMatchingService> _logger;

    public PlateMatchingService(ILogger<PlateMatchingService> logger)
    {
        _logger = logger;
    }

    public bool IsNameMatch(string registration, string name)
    {
        if (string.IsNullOrWhiteSpace(registration) || string.IsNullOrWhiteSpace(name))
            return false;

        // Consider it a match if score is above threshold
        // Lower threshold to 0.5 to accommodate partial matches
        return CalculateMatchScore(registration, name) >= 0.5;
    }

    public double CalculateMatchScore(string registration, string name)
    {
        if (string.IsNullOrWhiteSpace(registration) || string.IsNullOrWhiteSpace(name))
            return 0;

        var cleanRegistration = CleanString(registration);
        var cleanName = CleanString(name);

        // Try multiple matching strategies and return the best score
        // Note: Initials matching uses the original name (before cleaning) to detect words
        var scores = new[]
        {
            CalculateDirectMatchScore(cleanRegistration, cleanName),
            CalculatePhoneticMatchScore(cleanRegistration, cleanName),
            CalculateInitialsMatchScore(cleanRegistration, name), // Use original name with spaces
            CalculateConsonantMatchScore(cleanRegistration, cleanName)
        };

        var bestScore = scores.Max();
        
        _logger.LogDebug(
            "Match scoring: Registration '{Registration}' vs Name '{Name}': Direct={Direct:F2}, Phonetic={Phonetic:F2}, Initials={Initials:F2}, Consonant={Consonant:F2}, Best={Best:F2}",
            registration, name, scores[0], scores[1], scores[2], scores[3], bestScore);

        return bestScore;
    }

    /// <summary>
    /// Direct character-by-character matching score.
    /// </summary>
    private double CalculateDirectMatchScore(string registration, string name)
    {
        if (name.Length == 0) return 0;

        int matches = 0;
        int regIndex = 0;

        foreach (char nameChar in name)
        {
            // Find this character in the remaining registration
            while (regIndex < registration.Length)
            {
                if (registration[regIndex] == nameChar)
                {
                    matches++;
                    regIndex++;
                    break;
                }
                regIndex++;
            }
        }

        return (double)matches / name.Length;
    }

    /// <summary>
    /// Phonetic matching using number-to-letter substitutions.
    /// Examples: "JAM 3S" matches "JAMES", "DA12 NNY" matches "DANNY"
    /// </summary>
    private double CalculatePhoneticMatchScore(string registration, string name)
    {
        if (name.Length == 0) return 0;

        // Generate phonetic variants of the registration
        var phoneticRegistration = GeneratePhoneticVariant(registration);
        
        int matches = 0;
        int regIndex = 0;

        foreach (char nameChar in name)
        {
            while (regIndex < phoneticRegistration.Length)
            {
                if (phoneticRegistration[regIndex] == nameChar)
                {
                    matches++;
                    regIndex++;
                    break;
                }
                regIndex++;
            }
        }

        return (double)matches / name.Length;
    }

    /// <summary>
    /// Initials-based matching for multiple words.
    /// Examples: "GSM 17H" matches "G SMITH", "JDW" matches "JOHN DOE WILLIAMS"
    /// </summary>
    private double CalculateInitialsMatchScore(string registration, string name)
    {
        // Clean registration but keep name with spaces to detect words
        var cleanReg = CleanString(registration);
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length <= 1) return 0; // Only apply for multi-word names

        var initials = string.Join("", words.Select(w => char.ToUpperInvariant(w[0])));
        if (initials.Length == 0) return 0;

        // Check if registration contains these initials in order
        int matches = 0;
        int regIndex = 0;

        foreach (char initial in initials)
        {
            while (regIndex < cleanReg.Length)
            {
                if (cleanReg[regIndex] == initial)
                {
                    matches++;
                    regIndex++;
                    break;
                }
                regIndex++;
            }
            
            // If we didn't find this initial, stop looking
            if (regIndex >= cleanReg.Length && matches < initials.Length)
                break;
        }

        // Return higher score if all initials match, scaled score otherwise
        if (matches == initials.Length)
        {
            // Perfect initials match - return high score (0.85) but not perfect
            // to allow direct/phonetic matches to score higher when appropriate
            return 0.85;
        }
        
        // Partial initials match - lower score
        return (double)matches / initials.Length * 0.5;
    }

    /// <summary>
    /// Consonant-based matching (removes vowels for phonetic similarity).
    /// Helps match names with different vowel patterns.
    /// </summary>
    private double CalculateConsonantMatchScore(string registration, string name)
    {
        var regConsonants = ExtractConsonants(registration);
        var nameConsonants = ExtractConsonants(name);

        if (nameConsonants.Length == 0) return 0;

        int matches = 0;
        int regIndex = 0;

        foreach (char consonant in nameConsonants)
        {
            while (regIndex < regConsonants.Length)
            {
                if (regConsonants[regIndex] == consonant)
                {
                    matches++;
                    regIndex++;
                    break;
                }
                regIndex++;
            }
        }

        return (double)matches / nameConsonants.Length * 0.8; // Slightly lower weight
    }

    /// <summary>
    /// Generates a phonetic variant by replacing numbers with their letter equivalents.
    /// </summary>
    private string GeneratePhoneticVariant(string input)
    {
        var result = new StringBuilder(input.Length);

        foreach (char c in input)
        {
            if (NumberToLetterMap.TryGetValue(c, out var letters))
            {
                // Use the first (most common) letter mapping
                result.Append(letters[0]);
            }
            else
            {
                result.Append(c);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Extracts consonants from a string, removing vowels.
    /// </summary>
    private string ExtractConsonants(string input)
    {
        var vowels = new HashSet<char> { 'A', 'E', 'I', 'O', 'U' };
        return new string(input.Where(c => !vowels.Contains(c)).ToArray());
    }

    /// <summary>
    /// Cleans a string by removing spaces, hyphens, and converting to uppercase.
    /// </summary>
    private string CleanString(string input)
    {
        return Regex.Replace(input.ToUpperInvariant(), @"[^A-Z0-9]", "");
    }
}
