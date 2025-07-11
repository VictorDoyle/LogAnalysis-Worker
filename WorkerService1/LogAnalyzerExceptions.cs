using System;

namespace WorkerService1
{
    public class LogFileNotFoundException : Exception
    {
        public LogFileNotFoundException(string path)
            : base($"Log file not found: {path}") { }
    }

    public class LogFileReadException : Exception
    {
        public LogFileReadException(string path, Exception inner)
            : base($"Failed to read log file: {path}", inner) { }
    }

    public class LogFileParseException : Exception
    {
        public LogFileParseException(string message, Exception inner)
            : base(message, inner) { }
    }
}