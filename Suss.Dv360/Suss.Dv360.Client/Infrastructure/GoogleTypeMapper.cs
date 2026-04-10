using GoogleData = Google.Apis.DisplayVideo.v4.Data;

namespace Suss.Dv360.Client.Infrastructure;

/// <summary>
/// Utility class for converting between .NET <see cref="DateOnly"/> values and the
/// Google SDK’s <see cref="GoogleData.Date"/> type used by the Display &amp; Video 360 API.
/// <para>
/// The Google SDK represents dates as separate Year/Month/Day nullable integers rather
/// than a single date type. These helpers centralize the conversion logic so that
/// service classes remain clean.
/// </para>
/// </summary>
internal static class GoogleTypeMapper
{
    /// <summary>
    /// Converts a .NET <see cref="DateOnly"/> to a Google SDK <see cref="GoogleData.Date"/>.
    /// </summary>
    /// <param name="date">The date to convert, or <c>null</c>.</param>
    /// <returns>The equivalent Google date, or <c>null</c> if <paramref name="date"/> is <c>null</c>.</returns>
    public static GoogleData.Date? ToGoogleDate(DateOnly? date)
    {
        if (date is null) return null;

        // Map individual Year/Month/Day components from the DateOnly value.
        return new GoogleData.Date
        {
            Year = date.Value.Year,
            Month = date.Value.Month,
            Day = date.Value.Day
        };
    }

    /// <summary>
    /// Converts a Google SDK <see cref="GoogleData.Date"/> to a .NET <see cref="DateOnly"/>.
    /// </summary>
    /// <param name="date">The Google date to convert, or <c>null</c>.</param>
    /// <returns>The equivalent <see cref="DateOnly"/>, or <c>null</c> if any component is missing.</returns>
    public static DateOnly? FromGoogleDate(GoogleData.Date? date)
    {
        // All three components must be present to construct a valid DateOnly.
        if (date?.Year is null || date.Month is null || date.Day is null)
            return null;

        return new DateOnly(date.Year.Value, date.Month.Value, date.Day.Value);
    }
}
