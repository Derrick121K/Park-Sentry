using ParkSentry.Application.Interfaces.Scanning;
using ParkSentry.Domain.Helpers;

namespace ParkSentry.Application.Services;

/// <summary>
/// Orchestrates licence-disc scanning without pretending camera capture equals OCR.
/// </summary>
public sealed class ScanningPipelineService
{
    private readonly ILicenceDiscScanner _scanner;

    public ScanningPipelineService(ILicenceDiscScanner scanner) => _scanner = scanner;

    public string ActiveProvider => _scanner.ProviderName;
    public bool IsDemoProvider => _scanner.IsDemo;
    public bool SupportsOcr => _scanner.SupportsOcr;

    public async Task<ScanResult> CaptureAsync(ScanRequest request, CancellationToken ct = default)
    {
        var result = await _scanner.ScanAsync(request, ct);
        if (result.Success && !string.IsNullOrWhiteSpace(result.RegistrationNumber))
        {
            var normalized = RegistrationNormalizer.Normalize(result.RegistrationNumber);
            return result with
            {
                RegistrationNumber = normalized,
                TimestampUtc = result.TimestampUtc ?? DateTime.UtcNow,
                ScanId = result.ScanId ?? Guid.NewGuid()
            };
        }

        return result with
        {
            TimestampUtc = result.TimestampUtc ?? DateTime.UtcNow,
            ScanId = result.ScanId ?? Guid.NewGuid()
        };
    }

    public ScanResult ManualEntry(string input)
    {
        var parsed = _scanner.ParseManualInput(input);
        return parsed with
        {
            Status = ScanStatus.ManualFallback,
            ScanProvider = ScanProvider.Manual,
            IsOcrResult = false,
            Provider = "Manual",
            TimestampUtc = DateTime.UtcNow,
            ScanId = Guid.NewGuid()
        };
    }
}
