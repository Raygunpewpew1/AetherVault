using AetherVault.Core;
using AetherVault.Models;
using AetherVault.Services.DeckBuilder;

namespace AetherVault.Tests;

public class DeckSynergyTests
{
    [Fact]
    public void BuildProfileAndRoles_AggregatesSubtypesAndKeywords()
    {
        var elf = new Card
        {
            Uuid = "e1",
            Name = "Elf",
            CardType = "Creature — Elf Warrior",
            Subtypes = "Elf,Warrior",
            Keywords = "Reach",
            Text = ""
        };
        var goblin = new Card
        {
            Uuid = "g1",
            Name = "Goblin",
            CardType = "Creature — Goblin",
            Subtypes = "Goblin",
            Keywords = "Flying",
            Text = ""
        };
        var map = new Dictionary<string, Card>(StringComparer.OrdinalIgnoreCase)
        {
            [elf.Uuid] = elf,
            [goblin.Uuid] = goblin
        };
        var entities = new List<DeckCardEntity>
        {
            new() { CardId = elf.Uuid, Quantity = 2, Section = "Main" },
            new() { CardId = goblin.Uuid, Quantity = 1, Section = "Main" }
        };

        var stats = new DeckStats();
        var profile = DeckCohesionAnalyzer.BuildProfileAndRoles(entities, map, stats);

        Assert.Equal(2, profile.SubtypeTotals.GetValueOrDefault("Elf"));
        Assert.Equal(1, profile.SubtypeTotals.GetValueOrDefault("Goblin"));
        Assert.Equal(2, profile.SubtypeTotals.GetValueOrDefault("Warrior"));
        Assert.Equal(2, profile.KeywordTotals.GetValueOrDefault("Reach"));
        Assert.Equal(1, profile.KeywordTotals.GetValueOrDefault("Flying"));
    }

    [Fact]
    public void FormatOverlapHint_CountsOtherCardsBySubtype()
    {
        var a = new Card { Uuid = "a", Name = "A", Subtypes = "Elf", CardType = "Creature — Elf", Text = "" };
        var b = new Card { Uuid = "b", Name = "B", Subtypes = "Elf", CardType = "Creature — Elf", Text = "" };
        var c = new Card { Uuid = "c", Name = "C", Subtypes = "Goblin", CardType = "Creature — Goblin", Text = "" };
        var map = new Dictionary<string, Card>(StringComparer.OrdinalIgnoreCase)
        {
            [a.Uuid] = a,
            [b.Uuid] = b,
            [c.Uuid] = c
        };
        var entities = new List<DeckCardEntity>
        {
            new() { CardId = a.Uuid, Quantity = 1, Section = "Main" },
            new() { CardId = b.Uuid, Quantity = 2, Section = "Main" },
            new() { CardId = c.Uuid, Quantity = 1, Section = "Main" }
        };

        var hint = DeckCohesionAnalyzer.FormatOverlapHint(a, entities, map);
        Assert.NotNull(hint);
        Assert.Contains("2 deck cards share a subtype", hint);
    }

    [Fact]
    public void DeckCardRoleClassifier_CountsDrawFromText()
    {
        var stats = new DeckStats();
        var card = new Card
        {
            Uuid = "d1",
            Name = "Divination",
            CardType = "Sorcery",
            Text = "Draw two cards."
        };
        DeckCardRoleClassifier.AddRoleCounts(card, 3, stats);
        Assert.Equal(3, stats.CardDrawCount);
        Assert.Equal(0, stats.RampCount);
    }

    [Fact]
    public void DeckCardRoleClassifier_RampFromProducedMana()
    {
        var stats = new DeckStats();
        var card = new Card
        {
            Uuid = "m1",
            Name = "Dork",
            CardType = "Creature — Elf Druid",
            ProducedMana = "{G}",
            Text = ""
        };
        DeckCardRoleClassifier.AddRoleCounts(card, 1, stats);
        Assert.Equal(1, stats.RampCount);
    }

    [Fact]
    public void SearchOptions_Clone_IsIndependentCopy()
    {
        var a = new SearchOptions
        {
            SubtypeFilter = "Elf",
            KeywordsFilter = "Flying",
            UseLegalFormat = true,
            LegalFormat = DeckFormat.Commander
        };
        var b = a.Clone();
        Assert.Equal("Elf", b.SubtypeFilter);
        Assert.Equal("Flying", b.KeywordsFilter);
        Assert.True(b.UseLegalFormat);
        Assert.Equal(DeckFormat.Commander, b.LegalFormat);
        b.SubtypeFilter = "Goblin";
        Assert.Equal("Elf", a.SubtypeFilter);
    }
}
