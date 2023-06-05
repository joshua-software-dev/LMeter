using AngleSharp.Html.Dom;
using Dalamud.Game.Text;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;


namespace LMeter.Cactbot;

public class CactbotState
{
    public static CactbotState PreviewState = new (preview: true);
    public string? Alarm { get; private set; }
    public string? Alert { get; private set; }
    public string? Info { get; private set; }
    public event EventHandler? AlarmStateChanged = null;
    public event EventHandler? AlertStateChanged = null;
    public event EventHandler? InfoStateChanged = null;
    public ConcurrentDictionary<int, CactbotTimeLineElement> Timeline = new ();

    public CactbotState()
    {
        AlarmStateChanged += OnAlarmStateChange;
        AlertStateChanged += OnAlertStateChange;
        InfoStateChanged += OnInfoStateChange;
    }

    public CactbotState(bool preview)
    {
        Alarm = "ALARM!";
        Alert = "ALERT!";
        Info = "INFO!";

        Timeline[1] = new CactbotTimeLineElement(1);
        Timeline[2] = new CactbotTimeLineElement(2);
        Timeline[3] = new CactbotTimeLineElement(3);
    }

    private void OnAlarmStateChange(object? sender, EventArgs eventArgs)
    {
        if (!PluginManager.Instance.CactbotConfig.RaidbossAlarmsInChatEnabled || this.Alarm == null) return;

        var message = new XivChatEntry
        {
            Message = $"RAIDBOSS ALARM: {Alarm}",
            Type = XivChatType.ErrorMessage
        };
        PluginManager.Instance.ChatGui.PrintChat(message);
    }

    private void OnAlertStateChange(object? sender, EventArgs eventArgs)
    {
        if (!PluginManager.Instance.CactbotConfig.RaidbossAlertsInChatEnabled || this.Alert == null) return;

        var message = new XivChatEntry
        {
            Message = Alert,
            Name = "RAIDBOSS ALERT",
            Type = XivChatType.Yell
        };
        PluginManager.Instance.ChatGui.PrintChat(message);
    }

    private void OnInfoStateChange(object? sender, EventArgs eventArgs)
    {
        if (!PluginManager.Instance.CactbotConfig.RaidbossInfoInChatEnabled || this.Info == null) return;

        var message = new XivChatEntry
        {
            Message = Info,
            Name = "RAIDBOSS INFO",
            Type = XivChatType.NPCDialogueAnnouncements
        };
        PluginManager.Instance.ChatGui.PrintChat(message);
    }

    private void UpdateTimeline(IHtmlDocument html)
    {
        var timeline = html.GetElementById("timeline");
        if (timeline == null) return;

        var currentIds = new Dictionary<int, bool>();
        foreach (var container in timeline.GetElementsByClassName("timer-bar"))
        {
            if (container == null) continue;
            var parsedContainer = new CactbotTimeLineElement(container);

            if (Timeline.TryGetValue(parsedContainer.ContainerId, out var existingTimer))
            {
                existingTimer.Update(parsedContainer);
            }
            else
            {
                Timeline[parsedContainer.ContainerId] = parsedContainer;
            }

            currentIds[parsedContainer.ContainerId] = true;
        }

        // TODO: Find a way to remove multiple keys atomically. This works, but
        // only because there is only one other accessor, who exclusively reads
        // by making a complete copy of the keys whenever it iterates.
        foreach (var key in Timeline.Keys)
        {
            if (!currentIds.ContainsKey(key))
            {
                Timeline.TryRemove(key, out var _);
            }
        }
    }

    public void UpdateState(IHtmlDocument? html)
    {
        if (html == null)
        {
            Alarm = null;
            Alert = null;
            Info = null;
            Timeline.Clear();
            return;
        }

        UpdateTimeline(html);

        var alarmContainer = html.GetElementById("popup-text-alarm");
        var alarm = alarmContainer?.GetElementsByClassName("holder")?[0];
        var alertContainer = html.GetElementById("popup-text-alert");
        var alert = alertContainer?.GetElementsByClassName("holder")?[0];
        var infoContainer = html.GetElementById("popup-text-info");
        var info = infoContainer?.GetElementsByClassName("holder")?[0];

        var alarmWasEmpty = string.IsNullOrEmpty(Alarm);
        Alarm = alarm?.TextContent.Trim();
        if (alarmWasEmpty && !string.IsNullOrEmpty(Alarm))
        {
            AlarmStateChanged?.Invoke(this, EventArgs.Empty);
        }
        else if (Alarm == null && alarmWasEmpty)
        {
            AlarmStateChanged?.Invoke(this, EventArgs.Empty);
        }

        var alertWasEmpty = string.IsNullOrEmpty(Alert);
        Alert = alert?.TextContent.Trim();
        if (alertWasEmpty && !string.IsNullOrEmpty(Alert))
        {
            AlertStateChanged?.Invoke(this, EventArgs.Empty);
        }
        else if (Alert == null && alertWasEmpty)
        {
            AlertStateChanged?.Invoke(this, EventArgs.Empty);
        }

        var infoWasEmpty = string.IsNullOrEmpty(Info);
        Info = info?.TextContent.Trim();
        if (infoWasEmpty && !string.IsNullOrEmpty(Info))
        {
            InfoStateChanged?.Invoke(this, EventArgs.Empty);
        }
        else if (Info == null && infoWasEmpty)
        {
            InfoStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
