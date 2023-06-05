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
        var localPos = pos + config.RaidbossAlertsPosition;
        var size = config.RaidbossAlertsSize;

        DrawHelpers.DrawInWindow
        (
            name: "##___LMETER_CACTBOT_ALERTS",
            pos: localPos,
            size: size,
            needsInput: false,
            needsFocus: false,
            locked: true,
            drawAction: drawList =>
            {
                localPos = ImGui.GetWindowPos();
                config.RaidbossAlertsPosition = localPos - pos;

                size = ImGui.GetWindowSize();
                config.RaidbossAlertsSize = size;

                try
                {
                    if (config.RaidbossAlertsPreview)
                    {
                        ImGui.BeginChild("##CactbotAlertRender", new Vector2(size.X, size.Y), true);
                    }

                    using var bigFontScope = PluginManager.Instance.FontsManager
                        .PushFont(FontsManager.DefaultBigFontKey);

                    var cursorY = 0f;

                    var state = config.RaidbossAlertsPreview
                        ? CactbotState.PreviewState
                        : config.Cactbot.CactbotState;

                    if (config.RaidbossAlarmsEnabled && !string.IsNullOrEmpty(state.Alarm))
                    {
                        var textSize = ImGui.CalcTextSize(state.Alarm);
                        var textWidthOffset = (size.X - textSize.X) * 0.5f;
                        var textCenteredPos = localPos with
                        {
                            X = localPos.X + textWidthOffset,
                            Y = localPos.Y + cursorY
                        };

                        DrawHelpers.DrawText
                        (
                            drawList,
                            state.Alarm,
                            textCenteredPos,
                            4278190335, // red
                            config.RaidbossAlarmTextOutlineThickness > 0,
                            thickness: (int) config.RaidbossAlarmTextOutlineThickness
                        );

                        cursorY += textSize.Y + 10;
                    }

                    if (config.RaidbossAlertsEnabled && !string.IsNullOrEmpty(state.Alert))
                    {
                        var textSize = ImGui.CalcTextSize(state.Alert);
                        var textWidthOffset = (size.X - textSize.X) * 0.5f;
                        var textCenteredPos = localPos with
                        {
                            X = localPos.X + textWidthOffset,
                            Y = localPos.Y + cursorY
                        };

                        DrawHelpers.DrawText
                        (
                            drawList,
                            state.Alert,
                            textCenteredPos,
                            4278255615, // yellow
                            config.RaidbossAlertsTextOutlineThickness > 0,
                            thickness: (int) config.RaidbossAlertsTextOutlineThickness
                        );

                        cursorY += textSize.Y + 10;
                    }

                    if (config.RaidbossInfoEnabled && !string.IsNullOrEmpty(state.Info))
                    {
                        var textSize = ImGui.CalcTextSize(state.Info);
                        var textWidthOffset = (size.X - textSize.X) * 0.5f;
                        var textCenteredPos = localPos with
                        {
                            X = localPos.X + textWidthOffset,
                            Y = localPos.Y + cursorY
                        };

                        DrawHelpers.DrawText
                        (
                            drawList,
                            state.Info,
                            textCenteredPos,
                            4278255360, // green
                            config.RaidbossInfoTextOutlineThickness > 0,
                            thickness: (int) config.RaidbossInfoTextOutlineThickness
                        );

                        cursorY += textSize.Y + 10;
                    }
                }
                finally
                {
                    if (config.RaidbossAlertsPreview) ImGui.EndChild();
                }
            }
        );
    }

    public static void DrawTimeline(Vector2 pos)
    {
        var config = PluginManager.Instance.CactbotConfig;
        var localPos = pos + config.RaidbossTimelinePosition;
        var size = config.RaidbossTimelineSize;

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
                    if (config.RaidbossTimelinePreview)
                    {
                        ImGui.BeginChild("##CactbotTimelineRender", new Vector2(size.X, size.Y), true);
                    }

                    var windowWidth = ImGui.GetWindowSize().X;
                    var barWidth = windowWidth * 0.8f;
                    var progressBarSize = new Vector2(barWidth, 30);

                    var state = config.RaidbossTimelinePreview
                        ? CactbotState.PreviewState
                        : config.Cactbot.CactbotState;

                    foreach (var key in state.Timeline.Keys.OrderBy(it => it))
                    {
                        state.Timeline.TryGetValue(key, out var timelineInfo);
                        if (timelineInfo == null) continue;

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
                    if (config.RaidbossTimelinePreview) ImGui.EndChild();
                }
            }
        );
    }

    public static void Draw(Vector2 pos)
    {
        var config = PluginManager.Instance.CactbotConfig;
        if (!config.Enabled && !config.RaidbossAlertsPreview && !config.RaidbossTimelinePreview) return;

        config.Cactbot.PollingRate = CharacterState.IsInCombat()
            ? config.RaidbossInCombatPollingRate
            : config.RaidbossOutOfCombatPollingRate;

        DrawAlerts(pos);
        if (config.RaidbossTimelineEnabled)
        {
            DrawTimeline(pos);
        }
    }
}
