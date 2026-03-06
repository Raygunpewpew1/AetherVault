namespace AetherVault;

using AetherVault.Pages;
using Microsoft.Extensions.DependencyInjection;
using System;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;

    public App(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
    }

    /// <summary>Service provider for controls that need to resolve services (e.g. image loading).</summary>
    public static IServiceProvider? ServiceProvider => (Current as App)?._serviceProvider;

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var loadingPage = _serviceProvider.GetRequiredService<LoadingPage>();
        return new Window(loadingPage);
    }
}
