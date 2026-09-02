using ParkSentry.Application.Interfaces.Scanning;
using ParkSentry.Domain.Helpers;

namespace ParkSentry.Infrastructure.Scanning;

/// <summary>
/// DEMO / DEVELOPMENT SCANNER — No real OCR. Manual input only.
/// </summary>
public class DemoLicenceDiscScanner : ILicenceDiscScanner
{
    public string ProviderName => "DEMO / DEVELOPMENT SCANNER";
    public bool IsDemo => true;

    public Task<ScanResult> ScanAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new ScanResult(
            false, null, null, null,
            "Camera scanning is not available in demo mode. Please enter registration manually.",
            ProviderName));
    }

    public ScanResult ParseManualInput(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return new ScanResult(false, null, null, null, "Registration number is required.", ProviderName);

        var normalized = RegistrationNormalizer.Normalize(input);
        return new ScanResult(true, normalized, null, 1.0, null, ProviderName);
    }
}
