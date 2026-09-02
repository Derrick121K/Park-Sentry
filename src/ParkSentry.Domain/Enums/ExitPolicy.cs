namespace ParkSentry.Domain.Enums;

public enum ExitPolicy
{
    AllowWithOutstanding = 0,
    BlockUntilPaid = 1,
    WarnOnly = 2
}
