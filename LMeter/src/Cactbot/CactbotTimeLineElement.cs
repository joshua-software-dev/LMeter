using AngleSharp.Dom;
using ImGuiNET;
using System;
using System.Numerics;
using System.Text.RegularExpressions;


namespace LMeter.Cactbot;

public partial class CactbotTimeLineElement
{
    [GeneratedRegex("rgb\\(([0-9]{1,3})[\\w \\n]?\\,[\\w \\n]?([0-9]{1,3})[\\w \\n]?\\,[\\w \\n]?([0-9]{1,3})[\\w \\n]?\\)")]
    private static partial Regex _preCompiledRgbRegex();
    private readonly static Regex RgbRegex = _preCompiledRgbRegex();

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
    public uint? RgbValue;

    public CactbotTimeLineElement(int containerId)
    {
        ContainerId = containerId;
        Duration = 0;
        Value = 0;
        RgbValue = 4294936712; // rgb(136, 136, 255);
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
        RgbValue = GetStyleRgb(Fg);
    }

    private uint? GetStyleRgb(string? fg)
    {
        if (fg == null) return null;
        var match = RgbRegex.Match(fg);
        if (!match.Success) return null;

        var rgbValues = new ushort [3];
        var i = 0;
        foreach (var groupObj in match.Groups)
        {
            if (groupObj is Group group)
            {
                if (ushort.TryParse(group.Value, out var partialRgbNum))
                {
                    rgbValues[i] = partialRgbNum;
                    i += 1;
                }
            }
        }

        if (rgbValues.Length == 3)
        {
            return ImGui.GetColorU32(new Vector4(rgbValues[0] / 255f, rgbValues[1] / 255f, rgbValues[2] / 255f, 1f));
        }

        return null;
    }

    public void Update(CactbotTimeLineElement newlyParsed)
    {
        if (newlyParsed.Duration != Duration)
        {
            OriginalRemainingTime = TimeSpan.FromSeconds(newlyParsed.Value);
            ApproxCompletionTime = DateTime.Now + OriginalRemainingTime;
        }

        ContainerId = newlyParsed.ContainerId;
        ContainerStyle = newlyParsed.ContainerStyle;
        Duration = newlyParsed.Duration;
        Value = newlyParsed.Value;
        RightText = newlyParsed.RightText;
        LeftText = newlyParsed.LeftText;
        Toward = newlyParsed.Toward;
        StyleFill = newlyParsed.StyleFill;
        Fg = newlyParsed.Fg;
        RgbValue = GetStyleRgb(newlyParsed.Fg);
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
        RgbValue: {RgbValue}
        """;
}
