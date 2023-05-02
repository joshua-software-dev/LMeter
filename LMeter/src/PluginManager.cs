using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Interface;
using Dalamud.Plugin;
using ImGuiNET;
using LMeter.Act;
using LMeter.Config;
using LMeter.Helpers;
using LMeter.Meter;
using LMeter.Windows;
using System.Numerics;
using System;


namespace LMeter;

public class PluginManager : IDisposable
{
    private readonly Vector2 _origin = ImGui.GetMainViewport().Size / 2f;
    private readonly Vector2 _configSize = new (550, 550);
    private readonly ConfigWindow _configRoot;
    private readonly WindowSystem _windowSystem;
    private readonly ImGuiWindowFlags _mainWindowFlags = 
        ImGuiWindowFlags.NoTitleBar |
        ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.AlwaysAutoResize |
        ImGuiWindowFlags.NoBackground |
        ImGuiWindowFlags.NoInputs |
        ImGuiWindowFlags.NoBringToFrontOnFocus |
        ImGuiWindowFlags.NoSavedSettings;

    private readonly CommandManager _commandManager;
    private readonly LMeterConfig _config;

    public readonly ActClient ActClient;
    public readonly ClientState ClientState;
    public readonly Condition Condition;
    public readonly FontsManager FontsManager;
    public readonly DalamudPluginInterface PluginInterface;
    public readonly TexturesCache TexCache;

    public static PluginManager Instance { get; private set; } = null!;

    public PluginManager
    (
        ActClient actClient,
        ClientState clientState,
        CommandManager commandManager,
        Condition condition,
        LMeterConfig config,
        FontsManager fontsManager,
        DalamudPluginInterface pluginInterface,
        TexturesCache texCache
    )
    {
        PluginManager.Instance = this;
        
        ActClient = actClient;
        ClientState = clientState;
        _commandManager = commandManager;
        Condition = condition;
        _config = config;
        FontsManager = fontsManager;
        PluginInterface = pluginInterface;
        TexCache = texCache;

        _configRoot = new ConfigWindow(_config, "ConfigRoot", _origin, _configSize);
        _windowSystem = new WindowSystem("LMeter");
        _windowSystem.AddWindow(_configRoot);

        _commandManager.AddHandler(
            "/lm",
            new CommandInfo(PluginCommand)
            {
                HelpMessage = 
                    """
                    Opens the LMeter configuration window.
                    /lm end → Ends current ACT Encounter.
                    /lm clear → Clears all ACT encounter log data.
                    /lm ct <number> → Toggles clickthrough status for the given profile.
                    /lm toggle <number> [on|off] → Toggles visibility for the given profile.
                    """,
                ShowInHelp = true
            }
        );

        ClientState.Login += OnLogin;
        ClientState.Logout += OnLogout;
        PluginInterface.UiBuilder.OpenConfigUi += OpenConfigUi;
        PluginInterface.UiBuilder.Draw += Draw;
    }

    private void Draw()
    {
        if (ClientState.IsLoggedIn && (ClientState.LocalPlayer == null || CharacterState.IsCharacterBusy())) return;

        _windowSystem.Draw();

        _config.ActConfig.TryReconnect();
        _config.ActConfig.TryEndEncounter();

        ImGuiHelpers.ForceNextWindowMainViewport();
        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.SetNextWindowSize(ImGui.GetMainViewport().Size);
        if (ImGui.Begin("LMeter_Root", _mainWindowFlags))
        {
            foreach (var meter in _config.MeterList.Meters)
            {
                meter.Draw(_origin);
            }
        }

        ImGui.End();
    }

    public void Clear()
    {
        ActClient.Current.Clear();
        foreach (var meter in _config.MeterList.Meters)
        {
            meter.Clear();
        }
    }

    public void Edit(IConfigurable configItem) =>
        _configRoot.PushConfig(configItem);

    public void ConfigureMeter(MeterWindow meter)
    {
        if (!_configRoot.IsOpen)
        {
            this.OpenConfigUi();
            this.Edit(meter);
        }
    }

    private void OpenConfigUi()
    {
        if (!_configRoot.IsOpen) _configRoot.PushConfig(_config);
    }

    private void OnLogin(object? sender, EventArgs? args)
    {
        if (_config.ActConfig.WaitForCharacterLogin) ActClient.Current.Start();
    }

    private void OnLogout(object? sender, EventArgs? args) =>
        ConfigHelpers.SaveConfig(_config);

    private void PluginCommand(string command, string arguments)
    {
        switch (arguments)
        {
            case "end":
                ActClient.Current.EndEncounter();
                break;
            case "clear":
                this.Clear();
                break;
            case { } argument when argument.StartsWith("toggle"):
                _config.MeterList.ToggleMeter(GetIntArg(argument) - 1, GetBoolArg(argument, 2));
                break;
            case { } argument when argument.StartsWith("ct"):
                _config.MeterList.ToggleClickThrough(GetIntArg(argument) - 1);
                break;
            default:
                this.ToggleWindow();
                break;
        }
    }

    private static int GetIntArg(string argument)
    {
        var args = argument.Split(" ");
        return
            args.Length > 1 &&
            int.TryParse(args[1], out var num) 
                ? num 
                : 0;
    }

    private static bool? GetBoolArg(string argument, int index = 1)
    {
        var args = argument.Split(" ");
        if (args.Length > index)
        {
            var arg = args[index].ToLower();
            return
                arg.Equals("on")
                    ? true
                    : arg.Equals("off") 
                        ? false
                        : null;
        }

        return null;
    }

    private void ToggleWindow()
    {
        if (_configRoot.IsOpen)
        {
            _configRoot.IsOpen = false;
        }
        else
        {
            _configRoot.PushConfig(_config);
        }
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Don't modify order
            PluginInterface.UiBuilder.Draw -= Draw;
            PluginInterface.UiBuilder.OpenConfigUi -= OpenConfigUi;
            ClientState.Login -= OnLogin;
            ClientState.Logout -= OnLogout;
            _commandManager.RemoveHandler("/lm");
            _windowSystem.RemoveAllWindows();

            ActClient.Current.Dispose();
            _config.Dispose();
            FontsManager.Dispose();
            TexCache.Dispose();
        }
    }
}
