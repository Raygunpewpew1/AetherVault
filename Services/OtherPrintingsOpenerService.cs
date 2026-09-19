using AetherVault.Models;
using AetherVault.Pages;

namespace AetherVault.Services;

public sealed class OtherPrintingsOpenerService : IOtherPrintingsOpener
{
    private readonly IServiceProvider _serviceProvider;

    public OtherPrintingsOpenerService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<string?> PickAsync(IReadOnlyList<OtherPrintingSummary> printings)
    {
        if (printings.Count == 0)
            return null;

        var page = _serviceProvider.GetRequiredService<OtherPrintingsPage>();
        page.Init(printings);

        var currentPage = Shell.Current?.Navigation?.ModalStack.LastOrDefault()
                          ?? Shell.Current?.CurrentPage
                          ?? Application.Current?.Windows.FirstOrDefault()?.Page;

        if (currentPage is null)
            return null;

        await currentPage.Navigation.PushModalAsync(page, animated: true);
        return await page.WaitForResultAsync();
    }
}
