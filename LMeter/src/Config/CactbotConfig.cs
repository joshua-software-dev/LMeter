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
    public int InCombatPollingRate = 10;
    public int OutOfCombatPollingRate = 1000;
    public Vector2 AlertsPosition = new (-(ImGui.GetMainViewport().Size.Y * 12.5f / 90f), -(ImGui.GetMainViewport().Size.Y / 3.6f));
    public Vector2 AlertsSize = new (ImGui.GetMainViewport().Size.Y * 25 / 90, ImGui.GetMainViewport().Size.Y / 3.6f);
    [JsonIgnore]
    public bool AlertsPreview = false;

    public Vector2 TimelinePosition = new (-(ImGui.GetMainViewport().Size.Y * 70 / 90f), -(ImGui.GetMainViewport().Size.Y / 5f));
    public Vector2 TimelineSize = new (ImGui.GetMainViewport().Size.Y * 25 / 90, ImGui.GetMainViewport().Size.Y / 3.6f);
    [JsonIgnore]
    public bool TimelinePreview = false;


    public string Name =>
        "Cactbot";

    public CactbotConfig()
    {
        Cactbot = new (CactbotUrl, (ushort) HttpPort);
    }

    public IConfigPage GetDefault() =>
        new CactbotConfig();

    public void SetNewCactbotUrl()
    {
        Cactbot.SendShutdownCommand();
        Cactbot.Dispose();
        Cactbot = new (CactbotUrl, (ushort) HttpPort);
        Cactbot.StartBackgroundPolling();
    }

    public void DrawConfig(Vector2 size, float padX, float padY)
    {
        try
        {
            if (ImGui.BeginChild("##CactbotConfig", new Vector2(size.X, size.Y), true))
            {
                var tempAddress = this.CactbotUrl;
                ImGui.Text("Process Started: ");
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                if (Cactbot.PollingStarted)
                {
                    ImGui.Text("");
                }
                else
                {
                    ImGui.Text("");
                }
                ImGui.PopFont();

                ImGui.Text("Connection Active: ");
                ImGui.SameLine();
                ImGui.PushFont(UiBuilder.IconFont);
                if (Cactbot.LastPollSuccessful)
                {
                    ImGui.Text("");
                }
                else
                {
                    ImGui.Text("");
                }
                ImGui.PopFont();

                if (ImGui.Checkbox("Enabled", ref this.Enabled))
                {
                    if (!Cactbot.PollingStarted) Cactbot.StartBackgroundPolling();
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

                    ImGui.Text("[Change this if the default port conflicts with other things you have running]");
                    var tempPort = this.HttpPort;
                    var tempInRate = this.InCombatPollingRate;
                    var tempOutRate = this.OutOfCombatPollingRate;
                    ImGui.PushItemWidth(100);
                    ImGui.InputInt("HTTP Server Port", ref tempPort);
                    ImGui.InputInt("In Combat Polling Rate in milliseconds [Lower is faster]", ref tempInRate);
                    ImGui.InputInt("Out of Combat Polling Rate in milliseconds [Lower is faster]", ref tempOutRate);
                    ImGui.PopItemWidth();
                    tempPort = Math.Max(1, Math.Min(tempPort, ushort.MaxValue));
                    if (tempPort != this.HttpPort)
                    {
                        this.HttpPort = tempPort;
                    }

                    tempInRate = Math.Max(1, tempInRate);
                    if (tempInRate != this.InCombatPollingRate)
                    {
                        this.InCombatPollingRate = tempInRate;
                    }

                    tempOutRate = Math.Max(1, tempOutRate);
                    if (tempOutRate != this.OutOfCombatPollingRate)
                    {
                        this.OutOfCombatPollingRate = tempOutRate;
                    }

                    if (ImGui.Button("Retry Connection")) SetNewCactbotUrl();
                }

                var screenSize = ImGui.GetMainViewport().Size;
                ImGui.Text("Alerts Render Options:");
                ImGui.DragFloat2
                (
                    "Position##alerts",
                    ref this.AlertsPosition,
                    1,
                    -screenSize.X / 2,
                    screenSize.X / 2
                );

                ImGui.DragFloat2
                (
                    "Size##alerts",
                    ref this.AlertsSize,
                    1,
                    0,
                    screenSize.Y
                );

                ImGui.Checkbox("Preview##alerts", ref this.AlertsPreview);

                ImGui.Text("Timeline Render Options:");
                ImGui.DragFloat2
                (
                    "Position##timeline",
                    ref this.TimelinePosition,
                    1,
                    -screenSize.X / 2,
                    screenSize.X / 2
                );

                ImGui.DragFloat2
                (
                    "Size##timeline",
                    ref this.TimelineSize,
                    1,
                    0,
                    screenSize.Y
                );

                ImGui.Checkbox("Preview##timeline", ref this.TimelinePreview);
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
