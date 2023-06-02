using System;
using AngleSharp.Dom;


namespace LMeter.Cactbot;

public class CactbotTimeLineElement
{
    public int ContainerId;
    public string? ContainerStyle;
    public double Duration;
    public double Value;
    public string? RightText;
    public string? LeftText;
    public string? Toward;
    public string? StyleFill;
    public string? Fg;
    public TimeSpan OriginalRemainingTime;
    public DateTime ApproxCompletionTime;

    public CactbotTimeLineElement(int containerId)
    {
        ContainerId = containerId;
        Duration = 0;
        Value = 0;
    }

    public CactbotTimeLineElement(IElement container)
    {
        ContainerId = -1;
        if (int.TryParse(container.GetAttribute("id"), out var contId))
        {
            ContainerId = contId;
        }

        ContainerStyle = container.GetAttribute("style");
        var timerBar = container.GetElementsByTagName("timer-bar")[0];

        Duration = -1;
        if (double.TryParse(timerBar.GetAttribute("duration"), out var durationFloat))
        {
            Duration = durationFloat;
        }

        Value = -1;
        if (double.TryParse(timerBar.GetAttribute("value"), out var valueFloat))
        {
            Value = valueFloat;
        }

        RightText = timerBar.GetAttribute("righttext");
        LeftText = timerBar.GetAttribute("lefttext");
        Toward = timerBar.GetAttribute("toward");
        StyleFill = timerBar.GetAttribute("stylefill");
        Fg = timerBar.GetAttribute("fg");

        OriginalRemainingTime = TimeSpan.FromSeconds(Value);
        ApproxCompletionTime = DateTime.Now + OriginalRemainingTime;
    }

    public void Update(CactbotTimeLineElement newlyParsed)
    {
        ContainerId = newlyParsed.ContainerId;
        ContainerStyle = newlyParsed.ContainerStyle;
        Duration = newlyParsed.Duration;
        Value = newlyParsed.Value;
        RightText = newlyParsed.RightText;
        LeftText = newlyParsed.LeftText;
        Toward = newlyParsed.Toward;
        StyleFill = newlyParsed.StyleFill;
        Fg = newlyParsed.Fg;
    }

    public override string ToString() =>
        $"""
        ContainerId: {ContainerId}
        ContainerStyle: {ContainerStyle}
        Duration: {Duration}
        Value: {Value}
        RightText: {RightText}
        LeftText: {LeftText}
        Toward: {Toward}
        StyleFill: {StyleFill}
        Fg: {Fg}
        OriginalRemainingTime: {OriginalRemainingTime}
        ApproxCompletionTime: {ApproxCompletionTime}
        """;
}
