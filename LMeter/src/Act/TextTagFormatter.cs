using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System;


namespace LMeter.Act;

public partial class TextTagFormatter
{
    [GeneratedRegex(@"\[(\w*)(:k)?\.?(\d+)?\]", RegexOptions.Compiled)]
    private static partial Regex _textTagRegex();
    public static Regex TextTagRegex { get; } = _textTagRegex();

    private readonly string _format;
    private readonly Dictionary<string, FieldInfo> _fields;
    private readonly object _source;

    public TextTagFormatter(object source, string format, Dictionary<string, FieldInfo> fields) =>
        (_source, _format, _fields) = (source, format, fields);

    public string Evaluate(Match m)
    {
        if (m.Groups.Count != 4) return m.Value;

        var format = string.IsNullOrEmpty(m.Groups[3].Value)
            ? $"{_format}0"
            : $"{_format}{m.Groups[3].Value}";
            
        var key = m.Groups[1].Value;
        string? value = null;

        if (_fields.TryGetValue(key, out var fieldInfo))
        {
            object? propValue = fieldInfo.GetValue(_source);

            if (propValue is null) return string.Empty;

            if (propValue is LazyFloat lazyFloat)
            {
                var kilo = !string.IsNullOrEmpty(m.Groups[2].Value);
                value = lazyFloat.ToString(format, kilo) ?? m.Value;
            }
            else
            {
                value = propValue?.ToString();

                if 
                (
                    !string.IsNullOrEmpty(value) &&
                    int.TryParse(m.Groups[3].Value, out int trim) &&
                    trim < value.Length
                )
                {
                    value = propValue?.ToString().AsSpan(0, trim).ToString();
                }
            }
        }

        return value ?? m.Value;
    }
}
