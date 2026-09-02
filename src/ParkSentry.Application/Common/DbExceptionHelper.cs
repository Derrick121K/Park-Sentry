using Microsoft.EntityFrameworkCore;

namespace ParkSentry.Application.Common;

public static class DbExceptionHelper
{
    public static bool IsUniqueConstraintViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("unique constraint", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("23505", StringComparison.OrdinalIgnoreCase) == true;
}
