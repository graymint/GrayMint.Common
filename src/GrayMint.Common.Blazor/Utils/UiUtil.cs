using System.ComponentModel;
using System.Globalization;
using System.Text;

namespace GrayMint.Common.Blazor.Utils;

public static class UiUtil
{
    public const long Gigabyte = 1000 * 1000 * 1000;

    public static string FormatDate(DateTime? dateTime)
    {
        return dateTime != null ? dateTime.Value.ToLocalTime().ToString("d") : CommonResource.Never;
    }

    public static string FormatTime(DateTime? dateTime)
    {
        return dateTime != null ? dateTime.Value.ToLocalTime().ToString("g") : CommonResource.Never;
    }

    public static string FormatTimeAgo(DateTime? dateTime)
    {
        if (dateTime == null)
            return CommonResource.Never;

        var timeSpan = DateTime.Now - dateTime.Value.ToLocalTime();

        if (timeSpan.TotalSeconds < 60)
            return string.Format(CommonResource.TimeAgoSeconds, (int)timeSpan.TotalSeconds);

        if (timeSpan.TotalMinutes < 60)
            return string.Format(CommonResource.TimeAgoMinutes, (int)timeSpan.TotalMinutes);

        return timeSpan.TotalHours < 24
            ? string.Format(CommonResource.TimeAgoHours, (int)timeSpan.TotalHours)
            : string.Format(CommonResource.TimeAgoDays, (int)timeSpan.TotalDays);
    }

    public static string? GetDisplayName(string? name, Guid? defValue)
    {
        return GetDisplayName(name, defValue?.ToString());
    }

    public static string? GetDisplayName(string? name, string? defValue)
    {
        return string.IsNullOrEmpty(name) ? defValue : name;
    }

    public static string FormatMegaBytes(long? sizeMb, bool use1024 = false, bool round = false)
    {
        // use L to avoid overflow
        var mb = use1024 ? 1024L * 1024L : 1000_000L;
        return FormatBytes(sizeMb * mb, use1024, round);
    }

    public static string FormatBytes(long? size, bool use1024 = false, bool round = false)
    {
        if (size == null) return "-";
        var v = size.Value;
        var u = use1024 ? 1024d : 1000d;
        IReadOnlyList<string> units = ["B", "KB", "MB", "GB", "TB", "PB"];
        var i = 0; var d = (double)v;
        while (d >= u && i < units.Count - 1) { d /= u; i++; }
        if (round) d = Math.Round(d);
        return d.ToString("0.##") + " " + units[i];
    }

    public static string FormatBits(long? bytes)
    {
        if (bytes == null) return "*";
        var bits = bytes.Value * 8d;
        IReadOnlyList<string> units = ["b", "Kb", "Mb", "Gb", "Tb"];
        var i = 0;
        while (bits >= 1000 && i < units.Count - 1) { bits /= 1000; i++; }
        return bits.ToString("0.##") + " " + units[i];
    }

    public static string GetCountryFlagUrl(string? countryCode)
    {
        return countryCode is "*" or null
            ? "/images/icon-globe.png"
            : $"https://flagcdn.com/w20/{countryCode.ToLower()}.png";
    }

    // a flag or the globe beside text in a centred row: a block, so a tooltip around it adds no line
    // below it, and 1px up, as the row centres it on the lowercase letters, below the capitals' centre
    public const string FlagStyle = "display: block; position: relative; top: -1px";

    public static string GetMemoryInfo(long? free, long? total, bool roundTotal, bool withDetails)
    {
        if (total == null && free == null || total == 0)
            return "-";

        if (total == null || free == null) {
            return free != null
                ? string.Format(CommonResource.FreeMemory, FormatMemoryBytes(free, format: "0.0")) + " / -"
                : $"- / {FormatMemoryBytes(total, round: roundTotal, format: "0.#")}";
        }

        var used = total - free;
        var percent = (int)((float)used / total * 100);
        var detail = $"{FormatMemoryBytes(used, format: "0.0")} / {FormatMemoryBytes(total, round: roundTotal, format: "0.#")}";
        var result = withDetails
            ? $"{percent}% ({detail})"
            : $"{percent}%";

        return result;
    }

    public static string FormatMemoryBytes(long? size, bool use1024 = true, bool round = false, string format = "0.0")
    {
        return size != null ? FormatMemoryBytes(size.Value, use1024: use1024, round: round, format: format) : "-";
    }

    private static string FormatMemoryBytes(long size, bool use1024 = true, bool round = false, string format = "0.0")
    {
        var kb = use1024 ? (double)1024 : 1000;
        var mb = kb * kb;
        var gb = mb * kb;
        var value = round ? Math.Round(size / gb) : size / gb;
        return value.ToString(format) + " GB";
    }

    public static T ConvertValue<T>(object? value, string? name = null)
    {
        switch (value)
        {
            case T value1:
                return value1;

            case null:
                return default!;

            case string newValueString:
            {
                if (string.IsNullOrWhiteSpace(newValueString))
                    return default!;

                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (!converter.CanConvertFrom(newValueString.GetType()))
                    throw new InvalidCastException(
                        $"Cannot convert {newValueString.GetType()} to {typeof(T)}. Name: {name}");

                var converted = converter.ConvertFromString(newValueString)
                                ?? throw new InvalidCastException($"Cannot convert to {typeof(T)}. Name: {name}");

                return (T)converted;
            }
        }

        throw new InvalidCastException($"Cannot convert {value.GetType()} to {typeof(T)}. Name: {name}");
    }

    public static string FormatCode(string? value, int group = 4)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        var formattedValue = new StringBuilder();
        for (var i = 0; i < value.Length; i++) {
            if (i > 0 && i % group == 0)
                formattedValue.Append('-');
            formattedValue.Append(value[i]);
        }

        return formattedValue.ToString();
    }

    public static string ExpiredInDays(DateTime contextExpirationTime)
    {
        var days = Math.Floor((contextExpirationTime.ToLocalTime() - DateTime.Now).TotalDays);
        return days < 0 ? CommonResource.Expired : days.ToString(CultureInfo.CurrentCulture);
    }

    public static Guid? ParseGuidOrNull(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && Guid.TryParse(value, out var result) ? result : null;
    }
}