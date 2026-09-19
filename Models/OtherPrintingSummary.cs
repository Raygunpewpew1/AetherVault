using AetherVault.Core;

namespace AetherVault.Models;

/// <summary>Lightweight row for the card-detail other printings list.</summary>
public sealed class OtherPrintingSummary
{
    public string Uuid { get; init; } = "";
    public string SetCode { get; init; } = "";
    public string SetName { get; init; } = "";
    public string Number { get; init; } = "";
    public CardRarity Rarity { get; init; } = CardRarity.Common;
    public string SetReleaseDate { get; init; } = "";
    public bool IsCurrent { get; init; }
    public int CollectionQuantity { get; init; }
    public bool IsFoilOwned { get; init; }
    public bool IsEtchedOwned { get; init; }

    public bool IsOwned => CollectionQuantity > 0;
    public bool ShowOwnershipBadge => IsOwned;

    /// <summary>Short owned badge for chips and list rows.</summary>
    public string OwnershipBadge
    {
        get
        {
            if (CollectionQuantity <= 0)
                return "";

            var qty = CollectionQuantity == 1 ? "Owned" : $"×{CollectionQuantity}";
            if (IsFoilOwned && IsEtchedOwned)
                return $"{qty} · foil/etched";
            if (IsFoilOwned)
                return $"{qty} · foil";
            if (IsEtchedOwned)
                return $"{qty} · etched";
            return qty;
        }
    }

    /// <summary>Lowercase haystack for in-sheet search.</summary>
    public string SearchHaystack =>
        $"{TitleText} {DetailText} {SetCode} {SetName} {Number}".ToLowerInvariant();

    /// <summary>Primary chip label — set name when available.</summary>
    public string TitleText =>
        !string.IsNullOrWhiteSpace(SetName)
            ? SetName
            : !string.IsNullOrWhiteSpace(SetCode)
                ? SetCode.ToUpperInvariant()
                : "Unknown set";

    /// <summary>Secondary chip detail — set code, rarity, and release year.</summary>
    public string DetailText
    {
        get
        {
            var parts = new List<string>(3);
            if (!string.IsNullOrWhiteSpace(SetCode))
                parts.Add(SetCode.ToUpperInvariant());

            parts.Add(Rarity.ToString());

            var year = TryGetReleaseYear(SetReleaseDate);
            if (year is not null)
                parts.Add(year);

            return string.Join(" · ", parts);
        }
    }

    private static string? TryGetReleaseYear(string? releaseDate)
    {
        if (string.IsNullOrWhiteSpace(releaseDate) || releaseDate.Length < 4)
            return null;

        ReadOnlySpan<char> span = releaseDate.AsSpan();
        if (span.Length >= 4
            && char.IsAsciiDigit(span[0])
            && char.IsAsciiDigit(span[1])
            && char.IsAsciiDigit(span[2])
            && char.IsAsciiDigit(span[3]))
        {
            return releaseDate[..4];
        }

        return null;
    }
}
