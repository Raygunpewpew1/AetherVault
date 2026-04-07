using AetherVault.Models;
using Microcharts;
using SkiaSharp;

namespace AetherVault.Services;

/// <summary>
/// Builds Microcharts donut charts from <see cref="CollectionStats"/> for the Stats tab.
/// </summary>
public static class CollectionStatsCharts
{
    private static readonly SKColor CommonColor = SKColor.Parse("#C0C0C0");
    private static readonly SKColor UncommonColor = SKColor.Parse("#B0C4DE");
    private static readonly SKColor RareColor = SKColor.Parse("#FFD700");
    private static readonly SKColor MythicColor = SKColor.Parse("#FF8C00");

    private static readonly SKColor CreatureColor = SKColor.Parse("#03DAC5");
    private static readonly SKColor SpellColor = SKColor.Parse("#6200EE");
    private static readonly SKColor LandColor = SKColor.Parse("#4CAF50");

    private static readonly SKColor CaptionText = SKColor.Parse("#A0A0A0");
    private static readonly SKColor ValueText = SKColor.Parse("#F0F0F0");

    /// <summary>Returns false when there are no cards to chart.</summary>
    public static bool TryCreateDistributionCharts(CollectionStats stats, out DonutChart? rarityChart, out DonutChart? typeChart)
    {
        rarityChart = null;
        typeChart = null;
        if (stats.TotalCards <= 0)
            return false;

        var rarityEntries = BuildRarityEntries(stats);
        var typeEntries = BuildTypeEntries(stats);
        if (rarityEntries.Count == 0 || typeEntries.Count == 0)
            return false;

        rarityChart = CreateDonut(rarityEntries);
        typeChart = CreateDonut(typeEntries);
        return true;
    }

    private static DonutChart CreateDonut(IEnumerable<ChartEntry> entries)
    {
        var chart = new DonutChart
        {
            Entries = entries,
            HoleRadius = 0.62f,
            BackgroundColor = SKColors.Transparent,
            LabelColor = CaptionText,
            LabelTextSize = 26f,
            IsAnimated = false,
            Margin = 12f
        };
        return chart;
    }

    private static List<ChartEntry> BuildRarityEntries(CollectionStats s)
    {
        var list = new List<ChartEntry>(4);
        void Add(int count, string label, SKColor color)
        {
            if (count <= 0) return;
            list.Add(new ChartEntry(count)
            {
                Label = label,
                ValueLabel = count.ToString(),
                Color = color,
                TextColor = CaptionText,
                ValueLabelColor = ValueText
            });
        }

        Add(s.CommonCount, "Common", CommonColor);
        Add(s.UncommonCount, "Uncommon", UncommonColor);
        Add(s.RareCount, "Rare", RareColor);
        Add(s.MythicCount, "Mythic", MythicColor);
        return list;
    }

    private static List<ChartEntry> BuildTypeEntries(CollectionStats s)
    {
        var list = new List<ChartEntry>(3);
        void Add(int count, string label, SKColor color)
        {
            if (count <= 0) return;
            list.Add(new ChartEntry(count)
            {
                Label = label,
                ValueLabel = count.ToString(),
                Color = color,
                TextColor = CaptionText,
                ValueLabelColor = ValueText
            });
        }

        Add(s.CreatureCount, "Creatures", CreatureColor);
        Add(s.SpellCount, "Spells", SpellColor);
        Add(s.LandCount, "Lands", LandColor);
        return list;
    }
}
