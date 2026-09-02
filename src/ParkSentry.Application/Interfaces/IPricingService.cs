using ParkSentry.Domain.Entities;

namespace ParkSentry.Application.Interfaces;

public interface IPricingService
{
    Task<decimal> CalculateParkingFeeAsync(ParkingRate rate, DateTime entryTime, DateTime exitTime, CancellationToken cancellationToken = default);
    decimal CalculateSafetyFee(Organization organization, decimal parkingFee);
}
