using AetherVault.Core;
using Microsoft.Maui.Storage;

namespace AetherVault.Services;

/// <summary>Persisted choice of full vs compact MTG catalog (affects download path and DB filename).</summary>
public static class MtgCatalogPreferences
{
    private const string Key = "mtg_catalog_mode";
    private const string SetupKey = "mtg_catalog_setup_completed";

    public static MtgCatalogMode Mode
    {
        get => Preferences.Default.Get(Key, "full") == "lite" ? MtgCatalogMode.Lite : MtgCatalogMode.Full;
        set => Preferences.Default.Set(Key, value == MtgCatalogMode.Lite ? "lite" : "full");
    }

    /// <summary>True after the user has confirmed full vs compact on first launch (or migrated from an existing DB).</summary>
    public static bool CatalogSetupCompleted
    {
        get => Preferences.Default.Get(SetupKey, false);
        set => Preferences.Default.Set(SetupKey, value);
    }
}
