using ImGuiNET;
using LMeter.Helpers;
using System;
using System.Linq;
using System.Numerics;


namespace LMeter.Cactbot;

public class CactbotRaidbossWindows
{
    public static void DrawAlerts(Vector2 pos)
    {
        var config = PluginManager.Instance.CactbotConfig;
        var localPos = pos + config.AlertsPosition;
        var size = config.AlertsSize;

        DrawHelpers.DrawInWindow
        (
            name: "##___LMETER_CACTBOT_ALERTS",
            pos: localPos,
            size: size,
            needsInput: false,
            needsFocus: false,
            locked: true,
            drawAction: _ =>
            {
                localPos = ImGui.GetWindowPos();
                config.AlertsPosition = localPos - pos;

                size = ImGui.GetWindowSize();
                config.AlertsSize = size;

                var originalScale = ImGui.GetFont().Scale;

                try
                {
                    if (config.AlertsPreview)
                    {
                        ImGui.BeginChild("##CactbotAlertRender", new Vector2(size.X, size.Y), true);
                    }

                    ImGui.GetFont().Scale *= 2;
                    ImGui.PushFont(ImGui.GetFont());
                    var windowWidth = ImGui.GetWindowSize().X;

                    var state = config.AlertsPreview
                        ? CactbotState.PreviewState
                        : config.Cactbot.CactbotState;

                    if (state.Alarm != null)
                    {
                        // red
                        var textWidth = ImGui.CalcTextSize(state.Alarm).X;
                        ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
                        ImGui.TextColored(new Vector4(255, 0, 0, 255), state.Alarm);
                    }

                    if (state.Alert != null)
                    {
                        // yellow
                        var textWidth = ImGui.CalcTextSize(state.Alert).X;
                        ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
                        ImGui.TextColored(new Vector4(128, 128, 0, 255), state.Alert);
                    }

                    if (state.Info != null)
                    {
                        // green
                        var textWidth = ImGui.CalcTextSize(state.Info).X;
                        ImGui.SetCursorPosX((windowWidth - textWidth) * 0.5f);
                        ImGui.TextColored(new Vector4(0, 255, 0, 255), state.Info);
                    }
                }
                finally
                {
                    ImGui.GetFont().Scale = originalScale;
                    ImGui.PopFont();

                    if (config.AlertsPreview) ImGui.EndChild();
                }
            }
        );
    }

    public static void DrawTimeline(Vector2 pos)
    {
        var config = PluginManager.Instance.CactbotConfig;
        var localPos = pos + config.TimelinePosition;
        var size = config.TimelineSize;

        DrawHelpers.DrawInWindow
        (
            name: "##___LMETER_CACTBOT_TIMELINE",
            pos: localPos,
            size: size,
            needsInput: false,
            needsFocus: false,
            locked: true,
            drawAction: _ =>
            {
                try
                {
                    if (config.TimelinePreview)
                    {
                        ImGui.BeginChild("##CactbotTimelineRender", new Vector2(size.X, size.Y), true);
                    }

                    var windowWidth = ImGui.GetWindowSize().X;
                    var barWidth = windowWidth * 0.8f;
                    var progressBarSize = new Vector2(barWidth, 30);

                    var state = config.TimelinePreview
                        ? CactbotState.PreviewState
                        : config.Cactbot.CactbotState;

                    foreach (var pair in state.Timeline.ToArray().OrderBy(it => it.Key))
                    {
                        var timelineInfo = pair.Value;
                        var remainingTime = timelineInfo.ApproxCompletionTime - DateTime.Now;
                        var progress = (float)
                        (
                            remainingTime.TotalSeconds /
                            timelineInfo.OriginalRemainingTime.TotalSeconds
                        );
                        ImGui.SetCursorPosX((windowWidth - barWidth) * 0.5f);
                        if (timelineInfo.StyleFill == "fill")
                        {
                            ImGui.ProgressBar
                            (
                                1 - progress,
                                progressBarSize,
                                $"{timelineInfo.LeftText} : {remainingTime:mm\\:ss\\.ff}"
                            );
                        }
                        else
                        {
                            ImGui.ProgressBar
                            (
                                progress,
                                progressBarSize,
                                $"{timelineInfo.LeftText} : {remainingTime:mm\\:ss\\.ff}"
                            );
                        }
                    }
                }
                finally
                {
                    if (config.TimelinePreview) ImGui.EndChild();
                }
            }
        );
    }

    public static void Draw(Vector2 pos)
    {
        var config = PluginManager.Instance.CactbotConfig;
        if (!config.Enabled && !config.AlertsPreview && !config.TimelinePreview) return;

        config.Cactbot.PollingRate = CharacterState.IsInCombat()
            ? config.InCombatPollingRate
            : config.OutOfCombatPollingRate;

        DrawAlerts(pos);
        DrawTimeline(pos);
    }
}
