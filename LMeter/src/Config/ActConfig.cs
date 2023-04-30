using Dalamud.Interface;
using ImGuiNET;
using LMeter.Helpers;
using Newtonsoft.Json;
using System.Numerics;
using System;


namespace LMeter.Config;

public class ActConfig : IConfigPage
{
    [JsonIgnore]
    private const string _defaultSocketAddress = "ws://127.0.0.1:10501/ws";

    [JsonIgnore]
    private DateTime? LastCombatTime { get; set; } = null;

    [JsonIgnore]
    private DateTime? LastReconnectAttempt { get; set; } = null;

    public string Name => "ACT";
        
    public IConfigPage GetDefault() => new ActConfig();

    [JsonProperty("IINACTMode")]
    public bool IinactMode = false;
    [JsonProperty("ACTSocketAddress")]
    public string ActSocketAddress;

    public int EncounterHistorySize = 15;

    public bool AutoReconnect = false;
    public int ReconnectDelay = 30;

    [JsonProperty("ClearACT")]
    public bool ClearAct = false;
    public bool AutoEnd = false;
    public int AutoEndDelay = 3;

    public ActConfig() =>
        this.ActSocketAddress = _defaultSocketAddress;

    public void DrawConfig(Vector2 size, float padX, float padY)
    {
        if (ImGui.BeginChild($"##{this.Name}", new Vector2(size.X, size.Y), true))
        {
            ImGui.Text("ACT Connection Mode:");

            var newClientRequested = false;
            var iinactModeNum = IinactMode ? 1 : 0;

            newClientRequested |= ImGui.RadioButton("ACT WebSocket", ref iinactModeNum, 0);
            ImGui.SameLine();
            newClientRequested |= ImGui.RadioButton("IINACT", ref iinactModeNum, 1);

            IinactMode = iinactModeNum == 1;
            if (newClientRequested)
            {
                PluginManager.Instance.ActClient.GetNewActClient();
                PluginManager.Instance.ActClient.Current.Start();
            }
            
            PluginManager.Instance.ActClient.Current.DrawConnectionStatus();
            if (!IinactMode)
            {
                ImGui.InputTextWithHint
                (
                    "ACT Websocket Address",
                    $"Default: '{_defaultSocketAddress}'",
                    ref this.ActSocketAddress,
                    64
                );
            }

            var buttonSize = new Vector2(40, 0);
            DrawHelpers.DrawButton
            (
                string.Empty,
                FontAwesomeIcon.Sync,
                PluginManager.Instance.ActClient.Current.RetryConnection,
                "Reconnect",
                buttonSize
            );
            ImGui.SameLine();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1f);
            ImGui.Text("Retry Connection");
            ImGui.NewLine();
            ImGui.Checkbox("Automatically attempt to reconnect if connection fails", ref this.AutoReconnect);
            if (this.AutoReconnect)
            {
                DrawHelpers.DrawNestIndicator(1);
                ImGui.PushItemWidth(30);
                ImGui.InputInt("Seconds between reconnect attempts", ref this.ReconnectDelay, 0, 0);
                ImGui.PopItemWidth();
            }

            ImGui.NewLine();
            ImGui.PushItemWidth(30);
            ImGui.InputInt("Number of Encounters to save", ref this.EncounterHistorySize, 0, 0);
            ImGui.PopItemWidth();

            ImGui.NewLine();
            ImGui.Checkbox("Clear ACT when clearing LMeter", ref this.ClearAct);
            ImGui.Checkbox("Force ACT to end encounter after combat", ref this.AutoEnd);
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip
                (
                    """
                    It is recommended to disable ACT Command Sounds if you use this feature.
                    The option can be found in ACT under Options -> Sound Settings.
                    """
                );
            }
                
            if (this.AutoEnd)
            {
                DrawHelpers.DrawNestIndicator(1);
                ImGui.PushItemWidth(30);
                ImGui.InputInt("Seconds delay after combat", ref this.AutoEndDelay, 0, 0);
                ImGui.PopItemWidth();
            }

            ImGui.NewLine();
            DrawHelpers.DrawButton
            (
                string.Empty,
                FontAwesomeIcon.Stop,
                PluginManager.Instance.ActClient.Current.EndEncounter,
                null,
                buttonSize
            );
            ImGui.SameLine();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1f);
            ImGui.Text("Force End Combat");

            DrawHelpers.DrawButton
            (
                string.Empty,
                FontAwesomeIcon.Trash,
                PluginManager.Instance.Clear,
                null,
                buttonSize
            );
            ImGui.SameLine();
            ImGui.SetCursorPosY(ImGui.GetCursorPosY() - 1f);
            ImGui.Text("Clear LMeter");
        }
            
        ImGui.EndChild();
    }

    public void TryReconnect()
    {
        if 
        (
            this.LastReconnectAttempt.HasValue &&
            PluginManager.Instance.ActClient.Current.ConnectionIncompleteOrFailed()
        )
        {
            if 
            (
                this.AutoReconnect &&
                this.LastReconnectAttempt < DateTime.UtcNow - TimeSpan.FromSeconds(this.ReconnectDelay)
            )
            {
                PluginManager.Instance.ActClient.Current.RetryConnection();
                this.LastReconnectAttempt = DateTime.UtcNow;
            }
        }
        else
        {
            this.LastReconnectAttempt = DateTime.UtcNow;
        }
    }

    public void TryEndEncounter()
    {
        if (PluginManager.Instance.ActClient.Current.ClientReady())
        {
            if (this.AutoEnd && CharacterState.IsInCombat())
            {
                this.LastCombatTime = DateTime.UtcNow;
            }
            else if 
            (
                this.LastCombatTime is not null && 
                this.LastCombatTime < DateTime.UtcNow - TimeSpan.FromSeconds(this.AutoEndDelay)
            )
            {
                PluginManager.Instance.ActClient.Current.EndEncounter();
                this.LastCombatTime = null;
            }
        }
    }
}
