namespace MechanicalMilkshake.Extensions;

internal static class TimeSpanExtensions
{
    extension(TimeSpan timeSpan)
    {
        internal string Humanize()
        {
            (double value, string unit) = timeSpan switch
            {
                { TotalDays: >= 365 }  => (timeSpan.TotalDays / 365, "year"),
                { TotalDays: >= 30 }   => (timeSpan.TotalDays / 30, "month"),
                { TotalDays: >= 1 }    => (timeSpan.TotalDays, "day"),
                { TotalHours: >= 1 }   => (timeSpan.TotalHours, "hour"),
                { TotalMinutes: >= 1 } => (timeSpan.TotalMinutes, "minute"),
                _                      => (timeSpan.TotalSeconds, "second"),
            };

            int rounded = (int)Math.Round(value);
            return $"{rounded} {unit}{(rounded == 1 ? "" : "s")}";
        }
    }
}
