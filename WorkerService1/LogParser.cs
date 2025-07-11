using System;
using System.Globalization;

namespace WorkerService1
{
    public static class LogParser
    {
        // e.g -> log line: 2025-07-10 14:30:21 INFO User login successful for user123
        public static LogEntry? Parse(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;

            // split log into timestamp, level, message
            var firstSpace = line.IndexOf(' ');
            var secondSpace = line.IndexOf(' ', firstSpace + 1);
            if (firstSpace < 0 || secondSpace < 0)
                return null;

            var timestampStr = line.Substring(0, secondSpace);
            var rest = line.Substring(secondSpace + 1);

            var levelEnd = rest.IndexOf(' ');
            if (levelEnd < 0)
                return null;

            var level = rest.Substring(0, levelEnd);
            var message = rest.Substring(levelEnd + 1);

            if (!DateTime.TryParseExact(timestampStr, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var timestamp))
                return null;

            return new LogEntry(timestamp, level, message);
        }
    }
}