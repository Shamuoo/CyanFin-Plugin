using Jellyfin.Plugin.CyanFin.Services;
using MediaBrowser.Common.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.CyanFin;

/// <summary>
/// Registers CyanFin plugin services with the DI container.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServiceProvider applicationServiceProvider)
    {
        serviceCollection.AddSingleton<TrickplayService>();
        serviceCollection.AddSingleton<WatchPartyService>();
        serviceCollection.AddSingleton<NotificationService>();
        serviceCollection.AddSingleton<CustomMetadataService>();
        serviceCollection.AddHostedService<EventListenerService>();
    }
}
