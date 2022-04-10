﻿using Dalamud.CrystalTower.DependencyInjection;
using Dalamud.CrystalTower.UI;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Plugin;
using System;
using System.Net.Http;
using TextToTalk.Backends;
using TextToTalk.Exceptions;
using TextToTalk.Middleware;
using TextToTalk.UngenderedOverrides;
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace TextToTalk;

public class Services : IServiceProvider, IDisposable
{
    [PluginService]
    private static DalamudPluginInterface PluginInterface { get; set; } = null!;

    [PluginService]
    private static CommandManager Commands { get; set; } = null!;

    [PluginService]
    private static ClientState ClientState { get; set; } = null!;

    [PluginService]
    private static Framework Framework { get; set; } = null!;

    [PluginService]
    private static DataManager Data { get; set; } = null!;

    [PluginService]
    private static ChatGui Chat { get; set; } = null!;

    [PluginService]
    private static GameGui Gui { get; set; } = null!;

    [PluginService]
    private static KeyState Keys { get; set; } = null!;

    [PluginService]
    private static ObjectTable Objects { get; set; } = null!;

    private PluginServiceCollection serviceCollection;

    public object GetService(Type serviceType)
    {
        return this.serviceCollection.GetService(serviceType);
    }

    public T GetService<T>() where T : class
    {
        return this.serviceCollection.GetService<T>();
    }

    public void Dispose()
    {
        this.serviceCollection.Dispose();
    }

    public static Services Create(DalamudPluginInterface pi, PluginConfiguration config)
    {
        var services = pi.Create<Services>() ?? throw new ServiceException("Failed to initialize plugin services.");
        services.serviceCollection = new PluginServiceCollection();

        services.serviceCollection.AddService(PluginInterface, shouldDispose: false);
        services.serviceCollection.AddService(Commands, shouldDispose: false);
        services.serviceCollection.AddService(ClientState, shouldDispose: false);
        services.serviceCollection.AddService(Framework, shouldDispose: false);
        services.serviceCollection.AddService(Data, shouldDispose: false);
        services.serviceCollection.AddService(Chat, shouldDispose: false);
        services.serviceCollection.AddService(Gui, shouldDispose: false);
        services.serviceCollection.AddService(Keys, shouldDispose: false);
        services.serviceCollection.AddService(Objects, shouldDispose: false);

        var sharedState = new SharedState();
        var http = new HttpClient();
        var filters = new MessageHandlerFilters(services, config);
        var backendManager = new VoiceBackendManager(config, http, sharedState);

        services.serviceCollection.AddService(config);
        services.serviceCollection.AddService(sharedState);
        services.serviceCollection.AddService(http);
        services.serviceCollection.AddService(new UngenderedOverrideManager());
        services.serviceCollection.AddService(backendManager);
        services.serviceCollection.AddService(new RateLimiter(() =>
        {
            if (config.MessagesPerSecond == 0)
            {
                return long.MaxValue;
            }

            return (long)(1000f / config.MessagesPerSecond);
        }));
        services.serviceCollection.AddService(filters);
        services.serviceCollection.AddService(new TalkAddonHandler(ClientState, Gui, Data, filters, Objects, config, sharedState, backendManager));
        services.serviceCollection.AddService(new ChatMessageHandler(filters, Objects, config, sharedState));
        services.serviceCollection.AddService(new WindowManager(services.serviceCollection));

        return services;
    }
}