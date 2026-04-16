using AetherVault.Models;

namespace AetherVault.Core;

/// <summary>Helpers for filtering in-memory collection rows.</summary>
public static class CollectionFilterHelper
{
    /// <summary>
    /// Keeps <see cref="CollectionItem"/> rows in <paramref name="all"/> order, keeping only rows whose <see cref="CollectionItem.CardUuid"/> is in <paramref name="allowed"/>.
    /// </summary>
    /// <param name="all">Full collection list in display order (e.g. manual sort).</param>
    /// <param name="allowed">UUIDs that passed the current filter query.</param>
    public static CollectionItem[] IntersectPreservingOrder(CollectionItem[] all, IReadOnlySet<string> allowed)
    {
        if (all.Length == 0 || allowed.Count == 0)
            return [];

        var list = new List<CollectionItem>(Math.Min(all.Length, allowed.Count));
        foreach (var item in all)
        {
            if (allowed.Contains(item.CardUuid))
                list.Add(item);
        }

        return list.ToArray();
    }

    /// <summary>Collection-tab-only filters on <see cref="CollectionItem"/> rows (not part of <c>SearchOptions</c>).</summary>
    public static CollectionItem[] ApplyRowFilters(
        CollectionItem[] items,
        bool foilOnly,
        bool etchedOnly,
        int minQuantityInclusive)
    {
        if (items.Length == 0)
            return items;

        IEnumerable<CollectionItem> q = items;
        if (foilOnly)
            q = q.Where(static i => i.IsFoil);
        if (etchedOnly)
            q = q.Where(static i => i.IsEtched);
        if (minQuantityInclusive > 0)
            q = q.Where(i => i.Quantity >= minQuantityInclusive);
        return q.ToArray();
    }
}
