using AetherVault.Core;
using AetherVault.Models;
using AetherVault.Services.DeckBuilder;

namespace AetherVault.Tests;

public class DeckSuggestionTests
{
    [Fact]
    public void ComputeSuggestionScore_LandfallArchetype_BoostsLandfallCard()
    {
        var landfall = new Card { Name = "Test", Text = "Landfall — draw a card.", CardType = "Creature" };
        var vanilla = new Card { Name = "Bear", Text = "", CardType = "Creature" };
        var stats = new DeckStats();
        var profile = new DeckCohesionProfile();

        int sLand = DeckSuggestionService.ComputeSuggestionScore(landfall, CommanderArchetype.Landfall, null, stats, profile);
        int sBear = DeckSuggestionService.ComputeSuggestionScore(vanilla, CommanderArchetype.Landfall, null, stats, profile);

        Assert.True(sLand > sBear, "Landfall text should score higher for Landfall archetype.");
    }

    [Fact]
    public void ComputeSuggestionScore_BasicLand_IsExcluded()
    {
        var forest = new Card { Name = "Forest", CardType = "Basic Land — Forest" };
        var stats = new DeckStats();
        var profile = new DeckCohesionProfile();
        int s = DeckSuggestionService.ComputeSuggestionScore(forest, CommanderArchetype.Unknown, null, stats, profile);
        Assert.True(s < -1_000_000_000);
    }

    [Fact]
    public void IsCommanderLikeRules_IncludesDuel_NotUsesCommanderZone()
    {
        Assert.True(DeckFormat.Duel.IsCommanderLikeRules());
        Assert.False(DeckFormat.Duel.UsesCommanderZone());
    }
}
