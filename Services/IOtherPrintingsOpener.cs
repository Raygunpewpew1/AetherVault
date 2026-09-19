using AetherVault.Models;

namespace AetherVault.Services;

/// <summary>
/// Opens the searchable other-printings picker modal and returns the selected card UUID, or null if dismissed.
/// </summary>
public interface IOtherPrintingsOpener
{
    Task<string?> PickAsync(IReadOnlyList<OtherPrintingSummary> printings);
}
