using ErrorOr;

namespace Application.PortfolioEntries;

internal static class PortfolioEntryErrors
{
    // Two entries for the same asset + exchange at the same RecordedAt tie in the
    // valuation (it keeps the latest RecordedAt with no tie-break), so the second
    // one is refused. `existingEntryId` lets the client offer to edit that one.
    public static Error Duplicate(long existingEntryId) => Error.Conflict(
        "PortfolioEntry.Duplicate",
        "An entry for this cryptocurrency and exchange already exists at that date.",
        new Dictionary<string, object> { ["existingEntryId"] = existingEntryId });
}
