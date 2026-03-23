using AetherVault.Core;
using Microsoft.Maui.Storage;

namespace AetherVault.Services;

/// <summary>Persisted choice of full vs compact MTG catalog (affects download path and DB filename).</summary>
public static class MtgCatalogPreferences
{
    private const string Key = "mtg_catalog_mode";

    public static MtgCatalogMode Mode
    {
        get => Preferences.Default.Get(Key, "full") == "lite" ? MtgCatalogMode.Lite : MtgCatalogMode.Full;
        set => Preferences.Default.Set(Key, value == MtgCatalogMode.Lite ? "lite" : "full");
    }
}
