namespace DataGenerator;

// Injects the "defects" the pipeline is meant to be stress-tested against.
// Every special character is built from its numeric code point at runtime
// (not typed as a literal in source) so this file stays plain ASCII.
internal sealed class ChaosInjector
{
    private static readonly string[] BadChars = BuildBadChars();

    private static string[] BuildBadChars()
    {
        return
        [
            ((char)7).ToString(),                       // bell - non-printable ASCII
            ((char)31).ToString(),                      // unit separator - non-printable ASCII
            "\"",                                        // unescaped quote
            ((char)233).ToString(),                      // e with acute accent - multi-byte UTF-8
            ((char)241).ToString(),                      // n with tilde - multi-byte UTF-8
            new string([(char)0xD83D, (char)0xDE00]), // grinning face emoji - surrogate pair
            new string([(char)0xD83D, (char)0xDD25])  // fire emoji - surrogate pair
        ];
    }

    private static readonly string[] DateFormats = { "yyyy-MM-dd", "MM/dd/yyyy", "dd-MMM-yyyy", "EPOCH" };
    private static readonly string[] CorruptAmountValues = { "N/A", "NULL", "-99999", "12,500.00$" };

    private readonly Random _rnd;

    public ChaosInjector(Random rnd) => _rnd = rnd;

    public string Contaminate(string text, double rate)
    {
        if (_rnd.NextDouble() >= rate) return text;

        var junk = BadChars[_rnd.Next(BadChars.Length)];
        var pos = _rnd.Next(text.Length + 1);
        return text.Insert(pos, junk);
    }

    public string FormatDate(DateTime date)
    {
        var format = DateFormats[_rnd.Next(DateFormats.Length)];
        return format == "EPOCH"
            ? new DateTimeOffset(date).ToUnixTimeMilliseconds().ToString()
            : date.ToString(format);
    }

    public string FormatAmount(decimal amount, double corruptRate)
    {
        return _rnd.NextDouble() < corruptRate
            ? CorruptAmountValues[_rnd.Next(CorruptAmountValues.Length)]
            : amount.ToString("F2");
    }

    // Embeds a nested JSON object inside a single CSV cell, backslash-escaped.
    public static string BuildDeviceInfoCell(string ip, string os)
    {
        var json = $"{{\\\"ip\\\": \\\"{ip}\\\", \\\"os\\\": \\\"{os}\\\"}}";
        return $"\"{json}\"";
    }

    // Sometimes leaves a comma-separated list UNQUOTED on purpose - this is the
    // "structural delimiter inside an unquoted value" defect that breaks naive CSV parsers.
    public string BuildTagsCell(string[] tags)
    {
        var joined = string.Join(",", tags);
        return _rnd.NextDouble() < 0.5 ? joined : $"\"{joined}\"";
    }
}
