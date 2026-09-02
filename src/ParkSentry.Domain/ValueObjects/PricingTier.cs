namespace ParkSentry.Domain.ValueObjects;

public record PricingTier(int FromMinutes, int? ToMinutes, decimal Amount);
