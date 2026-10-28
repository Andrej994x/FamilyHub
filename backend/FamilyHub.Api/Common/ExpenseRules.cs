namespace FamilyHub.Api.Common;

/// <summary>Limits shared by the expense validators.</summary>
public static class ExpenseRules
{
    /// <summary>The largest amount (MKD) accepted for one expense.</summary>
    public const decimal MaxAmount = 10_000_000m;

    public static readonly DateOnly MinDate = new(2000, 1, 1);

    /// <summary>
    /// The latest date that is not in the future anywhere (UTC+14). The API only knows UTC, so
    /// comparing with the UTC date would reject today's expenses just after local midnight
    /// east of UTC (e.g. 00:30 in Skopje). The client restricts the picker to its local today.
    /// </summary>
    public static DateOnly LatestAllowedDate() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(14));

    public static bool HasAtMostTwoDecimals(decimal value) => decimal.Round(value, 2) == value;
}
