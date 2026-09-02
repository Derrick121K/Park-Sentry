namespace ParkSentry.Domain.Helpers;

public static class RegistrationNormalizer
{
    public static string Normalize(string registration)
    {
        if (string.IsNullOrWhiteSpace(registration))
            return string.Empty;

        return new string(registration
            .ToUpperInvariant()
            .Where(c => char.IsLetterOrDigit(c))
            .ToArray());
    }
}
