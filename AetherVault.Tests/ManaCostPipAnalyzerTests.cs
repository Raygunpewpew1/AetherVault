using AetherVault.Core;

namespace AetherVault.Tests;

public class ManaCostPipAnalyzerTests
{
    [Fact]
    public void Accumulate_Counts_WUBRG_And_Generic()
    {
        var c = new int[ManaCostPipAnalyzer.SlotCount];
        ManaCostPipAnalyzer.Accumulate("{2}{W}{U}", 1, c);
        Assert.Equal(1, c[0]); // W
        Assert.Equal(1, c[1]); // U
        Assert.Equal(1, c[5]); // 2 + generic
    }

    [Fact]
    public void Accumulate_Hybrid_Splits_Colors()
    {
        var c = new int[ManaCostPipAnalyzer.SlotCount];
        ManaCostPipAnalyzer.Accumulate("{G/U}", 3, c);
        Assert.Equal(3, c[4]); // G
        Assert.Equal(3, c[1]); // U
    }
}
