using AngleSharp.Html.Dom;
using System.Collections.Concurrent;
using System.Collections.Generic;


namespace LMeter.Cactbot;

public class CactbotState
{
    public static CactbotState PreviewState = new (preview: true);
    public string? Alarm { get; private set; }
    public string? Alert { get; private set; }
    public string? Info { get; private set; }
    public ConcurrentDictionary<int, CactbotTimeLineElement> Timeline = new ();

    public CactbotState() { }
    public CactbotState(bool preview)
    {
        Alarm = "ALARM!";
        Alert = "ALERT!";
        Info = "INFO!";

        Timeline[1] = new CactbotTimeLineElement(1);
        Timeline[2] = new CactbotTimeLineElement(2);
        Timeline[3] = new CactbotTimeLineElement(3);
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

        // TODO: THIS IS HACKY, FIX THIS
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

        Alarm = alarm?.TextContent.Trim();
        Alert = alert?.TextContent.Trim();
        Info = info?.TextContent.Trim();
    }
}
