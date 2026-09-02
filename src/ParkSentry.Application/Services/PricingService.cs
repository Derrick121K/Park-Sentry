using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Entities;

namespace ParkSentry.Application.Services;

public class PricingService : IPricingService
{
    public Task<decimal> CalculateParkingFeeAsync(ParkingRate rate, DateTime entryTime, DateTime exitTime, CancellationToken cancellationToken = default)
    {
        var duration = exitTime - entryTime;
        var totalMinutes = (int)Math.Ceiling(duration.TotalMinutes);

        if (rate.GracePeriodMinutes.HasValue && totalMinutes <= rate.GracePeriodMinutes.Value)
            return Task.FromResult(0m);

        var tiers = System.Text.Json.JsonSerializer.Deserialize<List<Domain.ValueObjects.PricingTier>>(rate.TiersJson) ?? [];

        decimal fee = 0;
        if (tiers.Count == 0)
        {
            fee = Math.Ceiling(totalMinutes / 60m) * 10m;
        }
        else
        {
            foreach (var tier in tiers.OrderBy(t => t.FromMinutes))
            {
                if (totalMinutes >= tier.FromMinutes && (tier.ToMinutes == null || totalMinutes <= tier.ToMinutes))
                {
                    fee = tier.Amount;
                    break;
                }
            }

            if (fee == 0)
                fee = tiers.Last().Amount;
        }

        if (rate.DailyMaximum.HasValue && fee > rate.DailyMaximum.Value)
            fee = rate.DailyMaximum.Value;

        return Task.FromResult(fee);
    }

    public decimal CalculateSafetyFee(Organization organization, decimal parkingFee)
    {
        if (!organization.SafetyFeeEnabled || organization.SafetyFeeType == Domain.Enums.SafetyFeeType.None)
            return 0;

        return organization.SafetyFeeType switch
        {
            Domain.Enums.SafetyFeeType.Fixed => organization.SafetyFeeAmount,
            Domain.Enums.SafetyFeeType.Percentage => Math.Round(parkingFee * organization.SafetyFeeAmount / 100m, 2),
            _ => 0
        };
    }
}
