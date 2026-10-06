using System;

namespace SubmarineVoyage.UI
{
    public static class DurationFormat
    {
        /// <summary>Short label such as "10 min", "1 h", "1 h 30 min" or "45 s".</summary>
        public static string Short(TimeSpan duration)
        {
            if (duration.TotalMinutes < 1) return $"{Math.Ceiling(duration.TotalSeconds)} s";

            var hours = (int)duration.TotalHours;
            var minutes = duration.Minutes;
            if (hours == 0) return $"{minutes} min";
            return minutes == 0 ? $"{hours} h" : $"{hours} h {minutes} min";
        }

        /// <summary>Countdown as mm:ss, rounded up so it never shows 00:00 while time remains.</summary>
        public static string Countdown(TimeSpan remaining)
        {
            var seconds = (int)Math.Ceiling(remaining.TotalSeconds);
            return $"{seconds / 60:00}:{seconds % 60:00}";
        }
    }
}
