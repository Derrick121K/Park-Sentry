namespace ParkSentry.Application.Interfaces.Scanning;

public record ScanResult(
    bool Success,
    string? RegistrationNumber,
    string? LicenceDiscNumber,
    double? Confidence,
    string? ErrorMessage,
    string Provider = "Demo",
    string? RawDetectedText = null);

public interface ILicenceDiscScanner
{
    string ProviderName { get; }
    bool IsDemo { get; }
    Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default);
    ScanResult ParseManualInput(string input);
}
