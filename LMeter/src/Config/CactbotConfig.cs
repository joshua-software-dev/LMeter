using Dalamud.Interface;
using ImGuiNET;
using LMeter.Cactbot;
using LMeter.Helpers;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Numerics;
using System.Threading;


namespace LMeter.Config;

public class CactbotConfig : IConfigPage, IDisposable
{
    [JsonIgnore]
    public TotallyNotCefCactbotHttpSource? Cactbot { get; private set; }

    public bool AutomaticallyStartBackgroundWebBrowser = false;
    public bool AutomaticallyDownloadBackgroundWebBrowser = true;
    public string? WebBrowserInstallLocation = MagicValues.DefaultTotallyNotCefInstallLocation;
    [JsonIgnore]
    private bool? WebBrowserInstallLocationContainsExe = null;

    [JsonProperty("Enabled")]
    public bool EnableConnection = false;
    public string CactbotUrl = MagicValues.DefaultCactbotUrl;
    public int HttpPort = 8080;
    public bool BypassCactbotWebSocketUsingIPC = true;

    public bool RaidbossEnableAudio = true;
    public int RaidbossInCombatPollingRate = 10;
    public int RaidbossOutOfCombatPollingRate = 1000;
    public bool RaidbossAlarmsEnabled = true;
    public bool RaidbossAlertsEnabled = true;
    public bool RaidbossInfoEnabled = true;
    public bool RaidbossAlarmsInChatEnabled = false;
    public bool RaidbossAlertsInChatEnabled = false;
    public bool RaidbossInfoInChatEnabled = false;
    public uint RaidbossAlarmTextOutlineThickness = 2;
    public uint RaidbossAlertsTextOutlineThickness = 2;
    public uint RaidbossInfoTextOutlineThickness = 2;
    public Vector2 RaidbossAlertsPosition = new (-(ImGui.GetMainViewport().Size.Y * 12.5f / 90f), -(ImGui.GetMainViewport().Size.Y / 3.6f));
    public Vector2 RaidbossAlertsSize = new (ImGui.GetMainViewport().Size.Y * 25 / 90, ImGui.GetMainViewport().Size.Y / 3.6f);
    [JsonIgnore]
    public bool RaidbossAlertsPreview = false;

    public bool RaidbossTimelineEnabled = true;
    public Vector2 RaidbossTimelinePosition = new (-(ImGui.GetMainViewport().Size.Y * 70 / 90f), -(ImGui.GetMainViewport().Size.Y / 5f));
    public Vector2 RaidbossTimelineSize = new (ImGui.GetMainViewport().Size.Y * 25 / 90, ImGui.GetMainViewport().Size.Y / 3.6f);
    [JsonIgnore]
    public bool RaidbossTimelinePreview = false;


    public string Name =>
        "Cactbot";

    public IConfigPage GetDefault() =>
        new CactbotConfig();

    public void SetNewCactbotUrl(bool forceStart)
    {
        forceStart = forceStart || this.AutomaticallyStartBackgroundWebBrowser;

        ThreadPool.QueueUserWorkItem
        (
            _ =>
            {
                this.Cactbot?.SendShutdownCommand();
                this.Cactbot?.Dispose();
                this.Cactbot = new
                (
                    WebBrowserInstallLocation ?? MagicValues.DefaultTotallyNotCefInstallLocation,
                    BypassCactbotWebSocketUsingIPC,
                    CactbotUrl,
                    (ushort) HttpPort,
                    RaidbossEnableAudio
                );
                this.Cactbot.StartBackgroundPollingThread(forceStart);
            }
        );
    }

    private void DrawBrowserSettings(Vector2 windowSize)
    {
        using var browserScope = new DrawChildScope
        (
            "##BrowserSettings",
            windowSize with { X = windowSize.X * 0.94f, Y = 346 },
            true
        );
        if (!browserScope.Success) return;

        ImGui.Text("Web Browser Settings");
        ImGui.Separator();
        ImGui.Checkbox
        (
            "Automatically Start Background Web Browser",
            ref this.AutomaticallyStartBackgroundWebBrowser
        );
        ImGui.Checkbox
        (
            """
            Enable Audio
            [Web Browser must restart for setting to take effect]
            """,
            ref RaidbossEnableAudio
        );
        ImGui.Checkbox
        (
            """
            Bypass Cactbot WebSocket using IINACT IPC
            [Requires IINACT plugin to be installed]
            [May lower performance slightly]
            [Web Browser must restart for setting to take effect]
            """,
            ref this.BypassCactbotWebSocketUsingIPC
        );

        ImGui.Text("Background Web Browser State:");
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);

        switch (Cactbot?.WebBrowserState)
        {
            case TotallyNotCefBrowserState.NotStarted:
            {
                ImGui.Text(""); // Boxed X Mark
                break;
            }
            case TotallyNotCefBrowserState.Downloading:
            {
                ImGui.Text(""); // Loading Spinner
                break;
            }
            case TotallyNotCefBrowserState.Starting:
            {
                ImGui.Text(""); // Loading Spinner
                break;
            }
            case TotallyNotCefBrowserState.Running:
            {
                ImGui.Text(""); // Boxed checkmark
                break;
            }
            default:
            {
                ImGui.Text("?");
                break;
            }
        }
        ImGui.PopFont();
        ImGui.SameLine();
        ImGui.Text(Cactbot?.WebBrowserState.ToString() ?? "null");

        switch (Cactbot?.WebBrowserState)
        {
            case TotallyNotCefBrowserState.NotStarted:
            {
                if (ImGui.Button("Start Web Browser"))
                {
                    SetNewCactbotUrl(forceStart: true);
                }
                ImGui.SameLine();
                break;
            }
            case TotallyNotCefBrowserState.Downloading:
            {
                break;
            }
            case TotallyNotCefBrowserState.Starting:
            {
                break;
            }
            case TotallyNotCefBrowserState.Running:
            {
                if (ImGui.Button("Restart Web Browser"))
                {
                    SetNewCactbotUrl(forceStart: true);
                }
                ImGui.SameLine();
                break;
            }
            case TotallyNotCefBrowserState.NotRunning:
            {
                if (ImGui.Button("Restart Web Browser"))
                {
                    SetNewCactbotUrl(forceStart: true);
                }
                ImGui.SameLine();
                break;
            }
            default:
            {
                break;
            }
        }

        if (ImGui.Button("Kill Web Browser"))
        {
            Cactbot?.KillWebBrowserProcess();
            SetNewCactbotUrl(forceStart: false);
        }

        using var installScope = new DrawChildScope
        (
            "##InstallSettings",
            windowSize with { X = windowSize.X * 0.9f, Y = 108 },
            true
        );
        if (!installScope.Success) return;

        var tempLocation = this.WebBrowserInstallLocation;
        ImGui.Text("Browser Install Location:");
        ImGui.PushItemWidth(windowSize.X * 0.88f);
        if
        (
            ImGui.InputTextWithHint
            (
                "##exefolderpath",
                $"Default '{MagicValues.DefaultTotallyNotCefInstallLocation}'",
                ref tempLocation,
                64,
                ImGuiInputTextFlags.EnterReturnsTrue
            )
        )
        {
            this.WebBrowserInstallLocationContainsExe = null;
        }
        ImGui.PopItemWidth();

        ImGui.Text("TotallyNotCef.exe Found in Install Folder:");
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        this.WebBrowserInstallLocationContainsExe ??= File.Exists
        (
            Path.Join(this.WebBrowserInstallLocation, "TotallyNotCef.exe")
        );
        if (this.WebBrowserInstallLocationContainsExe.Value)
        {
            ImGui.Text(""); // Boxed checkmark
        }
        else
        {
            ImGui.Text(""); // Boxed X Mark
        }
        ImGui.PopFont();

        if (tempLocation != this.WebBrowserInstallLocation)
        {
            WebBrowserInstallLocation = tempLocation;
        }

        if (ImGui.Button("Reset to default location"))
        {
            this.WebBrowserInstallLocation = MagicValues.DefaultTotallyNotCefInstallLocation;
            this.WebBrowserInstallLocationContainsExe = null;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($"Default Location: '{MagicValues.DefaultTotallyNotCefInstallLocation}'");
        }
    }

    private void DrawConnectionSettings(Vector2 windowSize)
    {
        using var connectionScope = new DrawChildScope
        (
            "##ConnectionSettings",
            windowSize with { X = windowSize.X * 0.94f, Y = 85 + (this.EnableConnection ? 145 : 0) },
            true
        );
        if (!connectionScope.Success) return;

        ImGui.Text("Connection Settings");
        ImGui.Separator();
        ImGui.Checkbox("Enable Connection to Browser", ref this.EnableConnection);
        if (Cactbot != null)
        {
            Cactbot.ConnectionState = this.EnableConnection
                ? Cactbot.ConnectionState == TotallyNotCefConnectionState.Disabled
                    ? TotallyNotCefConnectionState.WaitingForConnection
                    : Cactbot.ConnectionState
                : TotallyNotCefConnectionState.Disabled;
        }

        ImGui.Text("Connection State:");
        ImGui.SameLine();
        ImGui.PushFont(UiBuilder.IconFont);
        switch (Cactbot?.ConnectionState)
        {
            case TotallyNotCefConnectionState.Disabled:
            {
                ImGui.Text(""); // Boxed X Mark
                break;
            }
            case TotallyNotCefConnectionState.WaitingForConnection:
            {
                ImGui.Text(""); // Loading Spinner
                break;
            }
            case TotallyNotCefConnectionState.AttemptingHandshake:
            {
                ImGui.Text(""); // Loading Spinner
                break;
            }
            case TotallyNotCefConnectionState.Connected:
            {
                ImGui.Text(""); // Boxed checkmark
                break;
            }
            case TotallyNotCefConnectionState.Disconnected:
            {
                ImGui.Text(""); // Boxed X Mark
                break;
            }
            default:
            {
                ImGui.Text("?");
                break;
            }
        }
        ImGui.PopFont();
        ImGui.SameLine();
        ImGui.Text(Cactbot?.ConnectionState.ToString() ?? "null");

        if (EnableConnection)
        {

            var tempAddress = this.CactbotUrl;
            ImGui.PushItemWidth(windowSize.X * 0.75f);
            ImGui.InputTextWithHint
            (
                "Cactbot URL",
                $"Default: '{MagicValues.DefaultCactbotUrl}'",
                ref tempAddress,
                1000
            );
            ImGui.PopItemWidth();
            if (tempAddress != this.CactbotUrl)
            {
                this.CactbotUrl = tempAddress;
            }

            var tempPort = this.HttpPort;
            using
            (
                var httpScope = new DrawChildScope
                (
                    "##HttpPort",
                    windowSize with { X = windowSize.X * 0.9f, Y = 60 },
                    true
                )
            )
            {
                if (httpScope.Success)
                {
                    ImGui.PushItemWidth(100);
                    ImGui.InputInt("HTTP Server Port", ref tempPort);
                    ImGui.PopItemWidth();
                    ImGui.Text("[Change this if the default port conflicts with other things you have running]");
                }
            }

            var tempInRate = this.RaidbossInCombatPollingRate;
            var tempOutRate = this.RaidbossOutOfCombatPollingRate;
            ImGui.PushItemWidth(100);
            ImGui.InputInt("In Combat Polling Rate in milliseconds [Lower is faster]", ref tempInRate);
            ImGui.InputInt("Out of Combat Polling Rate in milliseconds [Lower is faster]", ref tempOutRate);
            ImGui.PopItemWidth();
            tempPort = Math.Max(1, Math.Min(tempPort, ushort.MaxValue));
            if (tempPort != this.HttpPort)
            {
                this.HttpPort = tempPort;
            }

            tempInRate = Math.Max(1, tempInRate);
            if (tempInRate != this.RaidbossInCombatPollingRate)
            {
                this.RaidbossInCombatPollingRate = tempInRate;
            }

            tempOutRate = Math.Max(1, tempOutRate);
            if (tempOutRate != this.RaidbossOutOfCombatPollingRate)
            {
                this.RaidbossOutOfCombatPollingRate = tempOutRate;
            }
        }
    }

    private void DrawAlertSettings(Vector2 windowSize, Vector2 screenSize)
    {
        using var alertScope = new DrawChildScope
        (
            "##AlertSettings",
            windowSize with { X = windowSize.X * 0.94f, Y = 280 },
            true
        );
        if (!alertScope.Success) return;

        ImGui.Text("Alerts Render Options");
        ImGui.Separator();
        ImGui.Checkbox("Show Alarm popups", ref RaidbossAlarmsEnabled);
        ImGui.SameLine(200);
        ImGui.Checkbox("Show Alarm messages in game chat##alarms", ref RaidbossAlarmsInChatEnabled);

        ImGui.Checkbox("Show Alert popups", ref RaidbossAlertsEnabled);
        ImGui.SameLine(200);
        ImGui.Checkbox("Show Alert messages in game chat##alerts", ref RaidbossAlertsInChatEnabled);

        ImGui.Checkbox("Show Info popups", ref RaidbossInfoEnabled);
        ImGui.SameLine(200);
        ImGui.Checkbox("Show Info messages in game chat##info", ref RaidbossInfoInChatEnabled);

        ImGui.PushItemWidth(windowSize.X * 0.58f);
        ImGui.DragFloat2
        (
            "Position##alerts",
            ref this.RaidbossAlertsPosition,
            1,
            -screenSize.X / 2,
            screenSize.X / 2
        );

        ImGui.DragFloat2
        (
            "Size##alerts",
            ref this.RaidbossAlertsSize,
            1,
            0,
            screenSize.Y
        );

        var tempAlarmsOutline = (int) RaidbossAlarmTextOutlineThickness;
        ImGui.SliderInt("Alarms Text Outline Thickness", ref tempAlarmsOutline, 0, 10);
        RaidbossAlarmTextOutlineThickness = (uint) tempAlarmsOutline;

        var tempAlertsOutline = (int) RaidbossAlertsTextOutlineThickness;
        ImGui.SliderInt("Alerts Text Outline Thickness", ref tempAlertsOutline, 0, 10);
        RaidbossAlertsTextOutlineThickness = (uint) tempAlertsOutline;

        var tempInfoOutline = (int) RaidbossInfoTextOutlineThickness;
        ImGui.SliderInt("Info Text Outline Thickness", ref tempInfoOutline, 0, 10);
        RaidbossInfoTextOutlineThickness = (uint) tempInfoOutline;
        ImGui.PopItemWidth();

        ImGui.Checkbox("Preview##alerts", ref this.RaidbossAlertsPreview);
    }

    private void DrawTimelineSettings(Vector2 windowSize, Vector2 screenSize)
    {
        using var timelineScope = new DrawChildScope
        (
            "##TimelineSettings",
            windowSize with { X = windowSize.X * 0.94f, Y = 145 },
            true
        );
        if (!timelineScope.Success) return;

        ImGui.Text("Timeline Render Options");
        ImGui.Separator();
        ImGui.Checkbox("Show Timeline", ref RaidbossTimelineEnabled);
        ImGui.PushItemWidth(windowSize.X * 0.58f);
        ImGui.DragFloat2
        (
            "Position##timeline",
            ref this.RaidbossTimelinePosition,
            1,
            -screenSize.X / 2,
            screenSize.X / 2
        );

        ImGui.DragFloat2
        (
            "Size##timeline",
            ref this.RaidbossTimelineSize,
            1,
            0,
            screenSize.Y
        );
        ImGui.PopItemWidth();

        ImGui.Checkbox("Preview##timeline", ref this.RaidbossTimelinePreview);
    }

    public void DrawConfig(Vector2 size, float padX, float padY)
    {
        using var configScope = new DrawChildScope("##CactbotConfig", size, true);
        if (!configScope.Success) return;

        var windowSize = ImGui.GetWindowSize();
        var screenSize = ImGui.GetMainViewport().Size;
        DrawBrowserSettings(windowSize);
        DrawConnectionSettings(windowSize);
        DrawAlertSettings(windowSize, screenSize);
        DrawTimelineSettings(windowSize, screenSize);
    }

    public void Dispose()
    {
        this.Cactbot?.Dispose();
    }
}
