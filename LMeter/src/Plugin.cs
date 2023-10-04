using Dalamud.Data;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Interface;
using Dalamud.Logging;
using Dalamud.Plugin;
using ImGuiScene;
using LMeter.Act;
using LMeter.Config;
using LMeter.Helpers;
using LMeter.Meter;
using System.IO;
using System.Reflection;
using System;
using Dalamud.Interface.Internal;
using Dalamud.Plugin.Services;


namespace LMeter;

public class Plugin : IDalamudPlugin
{
    private readonly PluginManager _pluginManager;
    public static string Changelog { get; private set; } = string.Empty;
    public static string ConfigFileDir { get; private set; } = string.Empty;
    public const string ConfigFileName = "LMeter.json";
    public static string ConfigFilePath { get; private set; } = string.Empty;
    public static string? GitHash { get; private set; }
    public static IDalamudTextureWrap? IconTexture { get; private set; }
    public string Name => "LMeter";
    public static string? Version { get; private set; }

    public Plugin(
        IClientState clientState,
        ICommandManager commandManager,
        ICondition condition,
        DalamudPluginInterface pluginInterface,
        IDataManager dataManager,
        IChatGui chatGui
    )
    {
        LoadVersion();
        Plugin.ConfigFileDir = pluginInterface.GetPluginConfigDirectory();
        Plugin.ConfigFilePath = Path.Combine(pluginInterface.GetPluginConfigDirectory(), Plugin.ConfigFileName);

        // Init TexturesCache
        var texCache = new TexturesCache(dataManager, pluginInterface);

        // Load Icon Texure
        Plugin.IconTexture = LoadIconTexture(pluginInterface.UiBuilder);

        // Load changelog
        Plugin.Changelog = LoadChangelog();

        // Load config
        FontsManager.CopyPluginFontsToUserPath();
        LMeterConfig config = ConfigHelpers.LoadConfig(Plugin.ConfigFilePath);
        config.FontConfig.RefreshFontList();
        config.ApplyConfig();

        // Initialize Fonts
        var fontsManager = new FontsManager(pluginInterface.UiBuilder, config.FontConfig.Fonts.Values);

        // Connect to ACT
        var actClient = new ActClient(chatGui, config.ActConfig, pluginInterface);
        actClient.Current.Start();

        // Start the plugin
        _pluginManager = new PluginManager
        (
            actClient,
            chatGui,
            clientState,
            commandManager,
            condition,
            config,
            dataManager,
            fontsManager,
            pluginInterface,
            texCache
        );

        // Create profile on first load
        if (config.FirstLoad && config.MeterList.Meters.Count == 0)
        {
            config.MeterList.Meters.Add(MeterWindow.GetDefaultMeter("Profile 1"));
        }
        config.FirstLoad = false;
    }

    private static IDalamudTextureWrap? LoadIconTexture(UiBuilder uiBuilder)
    {
        var pluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(pluginPath)) return null;

        var iconPath = Path.Combine(pluginPath, "Media", "Images", "icon_small.png");
        if (!File.Exists(iconPath)) return null;

        try
        {
            return uiBuilder.LoadImage(iconPath);
        }
        catch (Exception ex)
        {
            PluginLog.Warning($"Failed to load LMeter Icon {ex}");
        }

        return null;
    }

    private static string LoadChangelog()
    {
        var pluginPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        if (string.IsNullOrEmpty(pluginPath)) return string.Empty;

        var changelogPath = Path.Combine(pluginPath, "Media", "Text", "changelog.md");
        if (File.Exists(changelogPath))
        {
            try
            {
                return File.ReadAllText(changelogPath).Replace("%", "%%");
            }
            catch (Exception ex)
            {
                PluginLog.Warning($"Error loading changelog: {ex}");
            }
        }

        return string.Empty;
    }

    private static void LoadVersion()
    {
        var assemblyVersion = (AssemblyInformationalVersionAttribute) Assembly
            .GetExecutingAssembly()
            .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)[0];

        (Plugin.Version, Plugin.GitHash) = assemblyVersion.InformationalVersion.Split("+") switch
        {
            [var versionNum, var gitHash] => (versionNum, gitHash),
            _ => throw new ArgumentException(nameof(assemblyVersion))
        };
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing) _pluginManager.Dispose();
    }
}
