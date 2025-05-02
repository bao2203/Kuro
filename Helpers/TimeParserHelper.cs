using System;
using System.Text.RegularExpressions;

// Design Notes:
// - Accepts floating point values (e.g., "1.25d", "5.5h").
// - Case-insensitive matching for units (e.g., "3H" or "2m").
// - Ignores invalid segments like "abc", "1x", or random text.
// - Allows stacking of units (e.g., "1h 30m 2h" => 3h30m).
// - Negative durations are ignored by design.
// - Will return false if total duration is 0 or all segments are invalid.
// - Doesn't support weeks/months to avoid calendar complexities.


namespace Kurohana.Helpers
{
    public static class TimeParserHelper
    {
        public static bool TryParseTime(string input, out TimeSpan timeSpan)
        {
            input = input.Trim();
            string pattern = @"(\d+(\.\d+)?)\s*([dhmsDHMS])";
            Regex regex = new Regex(pattern);
            MatchCollection matches = regex.Matches(input);
            timeSpan = TimeSpan.Zero;

            if (matches.Count == 0)
                return false;

            foreach (Match match in matches)
            {
                if (!match.Success)
                    continue;

                if (!double.TryParse(match.Groups[1].Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double value))
                    continue;

                if (value < 0)
                    continue; // Ignore negative durations

                char unit = char.ToLower(match.Groups[3].Value[0]);
                switch (unit)
                {
                    case 'd': timeSpan += TimeSpan.FromDays(value); break;
                    case 'h': timeSpan += TimeSpan.FromHours(value); break;
                    case 'm': timeSpan += TimeSpan.FromMinutes(value); break;
                    case 's': timeSpan += TimeSpan.FromSeconds(value); break;
                }
            }

            return timeSpan > TimeSpan.Zero;
        }

    }
}
