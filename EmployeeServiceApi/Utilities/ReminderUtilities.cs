namespace EmployeeSelfServiceApi.Utilities;

public static class ReminderUtilities
{
    public static TimeZoneInfo GetApplicationTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Africa/Lagos");
        }
        catch (TimeZoneNotFoundException)
        {
            // Windows timezone identifier for West Africa.
            return TimeZoneInfo.FindSystemTimeZoneById(
                "W. Central Africa Standard Time");
        }
    }

    public static DateOnly GetLagosDate(DateTime utcNow)
    {
        var utc = DateTime.SpecifyKind(
            utcNow,
            DateTimeKind.Utc);

        var lagosTime = TimeZoneInfo.ConvertTimeFromUtc(
            utc,
            GetApplicationTimeZone());

        return DateOnly.FromDateTime(lagosTime);
    }

    public static (DateTime StartUtc, DateTime EndUtc)
        GetUtcRangeForLagosDate(DateOnly date)
    {
        var timeZone = GetApplicationTimeZone();

        var localStart = date.ToDateTime(
            TimeOnly.MinValue,
            DateTimeKind.Unspecified);

        var localEnd = date.AddDays(1).ToDateTime(
            TimeOnly.MinValue,
            DateTimeKind.Unspecified);

        return (
            TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone),
            TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone)
        );
    }

    public static string BuildDeduplicationKey(
        int recipientEmployeeId,
        string itemType,
        int itemId,
        string reminderType,
        DateOnly reminderDate)
    {
        return $"{recipientEmployeeId}:{itemType}:{itemId}:" +
               $"{reminderType}:{reminderDate:yyyy-MM-dd}";
    }
}