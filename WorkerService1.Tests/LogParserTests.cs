using System;
using WorkerService1;
using Xunit;

namespace WorkerService1.Tests
{
    public class LogParserTests
    {
        [Fact]
        public void Parse_ValidLine_ReturnsLogEntry()
        {
            var line = "2025-07-10 14:30:21 INFO User login successful for user123";
            var entry = LogParser.Parse(line);
            Assert.NotNull(entry);
            Assert.Equal("INFO", entry.Level);
            Assert.Equal("User login successful for user123", entry.Message);
            Assert.Equal(new DateTime(2025, 7, 10, 14, 30, 21), entry.Timestamp);
        }

        [Fact]
        public void Parse_InvalidLine_ReturnsNull()
        {
            var line = "not a log line";
            var entry = LogParser.Parse(line);
            Assert.Null(entry);
        }
    }
}