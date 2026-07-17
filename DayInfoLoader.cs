using System;
using System.Globalization;
using UnityEngine;

public static class DayInfoLoader
{
    private enum Section
    {
        None,
        Rescue,
        Trash,
        Running,
        Distraction
    }

    public static bool TryLoad(
        int day,
        out DayInfo info,
        out string error)
    {
        info = null;
        error = string.Empty;

        TextAsset asset =
            Resources.Load<TextAsset>($"DayInfo/Day{day}");

        if (asset == null)
        {
            error = $"DayInfo/Day{day}.txt was not found.";
            return false;
        }

        return TryParse(asset.text, out info, out error);
    }

    public static bool TryParse(
        string text,
        out DayInfo info,
        out string error)
    {
        info = new DayInfo();
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Day info is empty.";
            return false;
        }

        Section section = Section.None;
        string[] lines = text.Split(
            new[] { '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries
        );

        foreach (string rawLine in lines)
        {
            string line = rawLine.Trim();

            if (TryReadSection(line, out Section nextSection))
            {
                section = nextSection;
                continue;
            }

            int separatorIndex = line.IndexOf(':');

            if (separatorIndex < 0)
                continue;

            string key = line.Substring(0, separatorIndex).Trim();
            string valueText = line.Substring(separatorIndex + 1).Trim();

            if (!float.TryParse(
                    valueText,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float value))
            {
                error = $"Invalid number: {line}";
                return false;
            }

            ApplyValue(info, section, key, value);
        }

        Normalize(info);

        if (info.dayTime <= 0f)
        {
            error = "Time must be greater than zero.";
            return false;
        }

        return true;
    }

    private static bool TryReadSection(
        string line,
        out Section section)
    {
        switch (line)
        {
            case "Rescue Spawn":
                section = Section.Rescue;
                return true;

            case "Trash Spawn":
                section = Section.Trash;
                return true;

            case "Running NPC Spawn":
                section = Section.Running;
                return true;

            case "Distraction Event":
                section = Section.Distraction;
                return true;

            default:
                section = Section.None;
                return false;
        }
    }

    private static void ApplyValue(
        DayInfo info,
        Section section,
        string key,
        float value)
    {
        if (key == "Time")
        {
            info.dayTime = value;
            return;
        }

        if (key == "Rescue Time")
        {
            info.rescueTimeLimit = value;
            return;
        }

        switch (section)
        {
            case Section.Rescue:
                SetRangeValue(
                    key,
                    value,
                    ref info.rescueMin,
                    ref info.rescueMax
                );
                break;

            case Section.Trash:
                SetRangeValue(
                    key,
                    value,
                    ref info.trashMin,
                    ref info.trashMax
                );
                break;

            case Section.Running:
                SetRangeValue(
                    key,
                    value,
                    ref info.runningMin,
                    ref info.runningMax
                );
                break;

            case Section.Distraction:
                if (key == "Enabled")
                {
                    info.distractionEnabled = value > 0f;
                }
                else if (key == "First Delay")
                {
                    info.distractionFirstDelay = value;
                }
                else
                {
                    SetRangeValue(
                        key,
                        value,
                        ref info.distractionMinRepeat,
                        ref info.distractionMaxRepeat
                    );
                }
                break;
        }
    }

    private static void SetRangeValue(
        string key,
        float value,
        ref float min,
        ref float max)
    {
        if (key == "Min")
            min = value;
        else if (key == "Max")
            max = value;
    }

    private static void Normalize(DayInfo info)
    {
        info.rescueTimeLimit =
            Mathf.Max(1f, info.rescueTimeLimit);

        NormalizeRange(ref info.rescueMin, ref info.rescueMax);
        NormalizeRange(ref info.trashMin, ref info.trashMax);
        NormalizeRange(ref info.runningMin, ref info.runningMax);
        info.distractionFirstDelay =
            Mathf.Max(0f, info.distractionFirstDelay);
        NormalizeRange(
            ref info.distractionMinRepeat,
            ref info.distractionMaxRepeat
        );
    }

    private static void NormalizeRange(ref float min, ref float max)
    {
        min = Mathf.Max(0f, min);
        max = Mathf.Max(0f, max);

        if (max < min)
            (min, max) = (max, min);
    }
}
