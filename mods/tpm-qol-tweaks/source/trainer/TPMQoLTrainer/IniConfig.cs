using System.Globalization;

namespace TPMQoLTrainer;

internal sealed class IniConfig
{
    private readonly List<string> _lines;

    private IniConfig(List<string> lines)
    {
        _lines = lines;
    }

    internal static IniConfig Load(string path)
    {
        return new IniConfig(File.ReadAllLines(path).ToList());
    }

    internal string Get(string section, string key, string fallback)
    {
        var current = string.Empty;

        foreach (var raw in _lines)
        {
            var line = raw.Trim();

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                current = line[1..^1].Trim();
                continue;
            }

            if (!current.Equals(section, StringComparison.OrdinalIgnoreCase))
                continue;

            var idx = line.IndexOf('=');
            if (idx < 0)
                continue;

            var candidate = line[..idx].Trim();
            if (candidate.Equals(key, StringComparison.OrdinalIgnoreCase))
                return line[(idx + 1)..].Trim();
        }

        return fallback;
    }

    internal float GetFloat(string section, string key, float fallback)
    {
        var raw = Get(section, key, fallback.ToString(CultureInfo.InvariantCulture));
        return float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    internal int GetInt(string section, string key, int fallback)
    {
        var raw = Get(section, key, fallback.ToString(CultureInfo.InvariantCulture));
        return int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    internal bool GetBool(string section, string key, bool fallback)
    {
        var raw = Get(section, key, fallback ? "true" : "false");
        return bool.TryParse(raw, out var value) ? value : fallback;
    }

    internal void Set(string section, string key, string value)
    {
        var sectionStart = -1;
        var sectionEnd = _lines.Count;
        var current = string.Empty;

        for (var i = 0; i < _lines.Count; i++)
        {
            var line = _lines[i].Trim();

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                var next = line[1..^1].Trim();

                if (sectionStart >= 0)
                {
                    sectionEnd = i;
                    break;
                }

                current = next;
                if (current.Equals(section, StringComparison.OrdinalIgnoreCase))
                    sectionStart = i;

                continue;
            }

            if (sectionStart < 0 || !current.Equals(section, StringComparison.OrdinalIgnoreCase))
                continue;

            var idx = line.IndexOf('=');
            if (idx < 0)
                continue;

            var candidate = line[..idx].Trim();
            if (!candidate.Equals(key, StringComparison.OrdinalIgnoreCase))
                continue;

            _lines[i] = $"{key} = {value}";
            return;
        }

        if (sectionStart >= 0)
        {
            _lines.Insert(sectionEnd, $"{key} = {value}");
            return;
        }

        if (_lines.Count > 0 && !string.IsNullOrWhiteSpace(_lines[^1]))
            _lines.Add(string.Empty);

        _lines.Add($"[{section}]");
        _lines.Add(string.Empty);
        _lines.Add($"{key} = {value}");
    }

    internal void SetFloat(string section, string key, decimal value)
    {
        Set(section, key, value.ToString("0.##", CultureInfo.InvariantCulture));
    }

    internal void SetInt(string section, string key, int value)
    {
        Set(section, key, value.ToString(CultureInfo.InvariantCulture));
    }

    internal void SetBool(string section, string key, bool value)
    {
        Set(section, key, value ? "true" : "false");
    }

    internal void Save(string path)
    {
        File.WriteAllLines(path, _lines, new System.Text.UTF8Encoding(false));
    }
}
