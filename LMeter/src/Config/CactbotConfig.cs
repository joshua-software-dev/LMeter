using Dalamud.Interface;
using ImGuiNET;
using LMeter.Cactbot;
using Newtonsoft.Json;
using System;
using System.Numerics;


namespace LMeter.Config;

public class CactbotConfig : IConfigPage, IDisposable
{
    [JsonIgnore]
    public TotallyNotCefCactbotHttpSource Cactbot { get; private set; }
    public bool Enabled = false;
    public string CactbotUrl = MagicValues.DefaultCactbotUrl;
    public int HttpPort = 8080;

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

    public CactbotConfig()
    {
        Cactbot = new (CactbotUrl, (ushort) HttpPort, RaidbossEnableAudio);
    }

    public IConfigPage GetDefault() =>
        new CactbotConfig();

    public void SetNewCactbotUrl(bool startPolling)
    {
        Cactbot.SendShutdownCommand();
        Cactbot.Dispose();
        Cactbot = new (CactbotUrl, (ushort) HttpPort, RaidbossEnableAudio);
        if (startPolling) Cactbot.StartBackgroundPolling();
    }

    public void DrawConfig(Vector2 size, float padX, float padY)
    {
        try
        {
            if (ImGui.BeginChild("##CactbotConfig", new Vector2(size.X, size.Y), true))
            {
                var tempAddress = this.CactbotUrl;
                ImGui.Text("Background Web Browser State: ");
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);

                switch (Cactbot.WebBrowserState)
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
                    case TotallyNotCefBrowserState.Connected:
                    {
                        ImGui.Text(""); // Boxed checkmark
                        break;
                    }
                    case TotallyNotCefBrowserState.Disconnected:
                    {
                        ImGui.Text(""); // Boxed X Mark
                        break;
                    }
                }
                ImGui.PopFont();
                ImGui.SameLine();
                ImGui.Text(Cactbot.WebBrowserState.ToString());

                if (ImGui.Checkbox("Enabled", ref this.Enabled))
                {
                    if (Cactbot.WebBrowserState == TotallyNotCefBrowserState.NotStarted)
                    {
                        Cactbot.StartBackgroundPolling();
                    }
                }

                if (Enabled)
                {
                    ImGui.PushItemWidth(ImGui.GetWindowWidth() * 0.8f);
                    ImGui.InputTextWithHint
                    (
                        "Cactbot URL",
                        $"Default: '{MagicValues.DefaultCactbotUrl}'",
                        ref tempAddress,
                        64
                    );
                    ImGui.PopItemWidth();
                    if (tempAddress != this.CactbotUrl)
                    {
                        this.CactbotUrl = tempAddress;
                    }

                    var tempPort = this.HttpPort;
                    var windowSize = ImGui.GetWindowSize();
                    try
                    {
                        if (ImGui.BeginChild("##HttpPort", windowSize with { X = windowSize.X * 0.9f, Y = 60 }, true))
                        {
                            ImGui.PushItemWidth(100);
                            ImGui.InputInt("HTTP Server Port", ref tempPort);
                            ImGui.PopItemWidth();
                            ImGui.Text("[Change this if the default port conflicts with other things you have running]");
                        }
                    }
                    finally
                    {
                        ImGui.EndChild();
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

                    ImGui.Checkbox("Enable Audio [Web Browser must restart for setting to take effect]", ref RaidbossEnableAudio);

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

                    if (ImGui.Button("Restart Web Browser")) SetNewCactbotUrl(true);
                }
                else if (!Enabled && Cactbot.WebBrowserState != TotallyNotCefBrowserState.NotStarted)
                {
                    SetNewCactbotUrl(false);
                }

                var screenSize = ImGui.GetMainViewport().Size;
                ImGui.NewLine();
                ImGui.Text("Alerts Render Options:");

                ImGui.Checkbox("Show Alarms popups", ref RaidbossAlarmsEnabled);
                ImGui.Checkbox("Show Alarms in game chat##alarms", ref RaidbossAlarmsInChatEnabled);
                ImGui.Checkbox("Show Alerts popups", ref RaidbossAlertsEnabled);
                ImGui.Checkbox("Show Alerts in game chat##alerts", ref RaidbossAlertsInChatEnabled);
                ImGui.Checkbox("Show Info popups", ref RaidbossInfoEnabled);
                ImGui.Checkbox("Show Info messages in game chat##info", ref RaidbossInfoInChatEnabled);

                ImGui.PushItemWidth(ImGui.GetWindowSize().X * 0.6f);
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

                ImGui.NewLine();
                ImGui.Text("Timeline Render Options:");
                ImGui.Checkbox("Show Timeline", ref RaidbossTimelineEnabled);
                ImGui.PushItemWidth(ImGui.GetWindowSize().X * 0.6f);
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
        }
        finally
        {
            ImGui.EndChild();
        }
    }

    public void Dispose()
    {
        this.Cactbot?.Dispose();
    }
}
