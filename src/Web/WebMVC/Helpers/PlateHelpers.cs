using System.Text.RegularExpressions;

namespace RTCodingExercise.Microservices.Helpers;

public static class PlateHelpers
{
    /// <summary>
    /// Formats a UK number plate registration with proper spacing according to UK standards
    /// </summary>
    /// <param name="registration">The registration to format</param>
    /// <returns>Formatted registration with proper spacing</returns>
    public static string FormatRegistration(string? registration)
    {
        if (string.IsNullOrWhiteSpace(registration))
            return registration ?? string.Empty;
            
        var clean = registration.Replace(" ", "").ToUpper();
        
        // Current format: AB12 ABC (2 letters, 2 numbers, 3 letters)
        if (Regex.IsMatch(clean, @"^[A-Z]{2}\d{2}[A-Z]{3}$"))
            return clean.Insert(4, " ");
            
        // Prefix format: A123 BCD (1 letter, 1-3 numbers, 3 letters)
        var prefixMatch = Regex.Match(clean, @"^([A-Z])(\d{1,3})([A-Z]{3})$");
        if (prefixMatch.Success)
            return $"{prefixMatch.Groups[1].Value}{prefixMatch.Groups[2].Value} {prefixMatch.Groups[3].Value}";
            
        // Suffix format: ABC 123D (3 letters, 1-3 numbers, 1 letter)
        var suffixMatch = Regex.Match(clean, @"^([A-Z]{3})(\d{1,3})([A-Z])$");
        if (suffixMatch.Success)
            return $"{suffixMatch.Groups[1].Value} {suffixMatch.Groups[2].Value}{suffixMatch.Groups[3].Value}";
            
        // Dateless/Cherished plates - try to find a sensible split point
        // Common patterns: AB 1234, A 1, ABC 1D, etc.
        var datelessMatch = Regex.Match(clean, @"^([A-Z]+)(\d+[A-Z]*)$");
        if (datelessMatch.Success)
            return $"{datelessMatch.Groups[1].Value} {datelessMatch.Groups[2].Value}";
            
        return registration; // Return as-is if no pattern matches
    }
}
