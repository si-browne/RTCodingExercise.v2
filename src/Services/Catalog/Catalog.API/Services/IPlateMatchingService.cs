namespace Catalog.API.Services;

/// <summary>
/// Service for advanced plate registration matching using phonetic and pattern-based algorithms.
/// Supports name-like matching (e.g., "Danny" matches "DA12 NNY", "James" matches "JAM 3S").
/// </summary>
public interface IPlateMatchingService
{
    /// <summary>
    /// Checks if a registration plate matches a given name using phonetic and pattern-based algorithms.
    /// </summary>
    /// <param name="registration">The plate registration to check</param>
    /// <param name="name">The name to match against</param>
    /// <returns>True if the plate matches the name pattern</returns>
    bool IsNameMatch(string registration, string name);

    /// <summary>
    /// Calculates a match score between 0 and 1 indicating how well a registration matches a name.
    /// </summary>
    /// <param name="registration">The plate registration to score</param>
    /// <param name="name">The name to match against</param>
    /// <returns>Match score from 0 (no match) to 1 (perfect match)</returns>
    double CalculateMatchScore(string registration, string name);
}
