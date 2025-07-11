using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using WorkerService1;
using Xunit;

namespace WorkerService1.Tests
{
    public class WorkerTests
    {
        [Fact]
        public async Task ProcessLogs_LogsWarning_WhenLogFilePathIsNull()
        {
            var logger = new Mock<ILogger<Worker>>();
            var config = new ConfigurationBuilder().Build(); // LogAnalyzer:LogFilePath not set

            var worker = new Worker(logger.Object, config);

            await worker.InvokeProcessLogsForTest();

            logger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Path is null or empty.")),
                    It.IsAny<LogFileNotFoundException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessLogs_LogsError_WhenFileDoesNotExist()
        {
            var logger = new Mock<ILogger<Worker>>();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("LogAnalyzer:LogFilePath", "nonexistent.log")
                })
                .Build();

            var worker = new Worker(logger.Object, config);

            await worker.InvokeProcessLogsForTest();

            logger.Verify(
                l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("nonexistent.log")),
                    It.IsAny<LogFileNotFoundException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessLogs_LogsError_WhenLineIsNull()
        {
            var logger = new Mock<ILogger<Worker>>();
            var filePath = "test_null_line.log";

            File.WriteAllLines(filePath, new string[] { "", "valid line" });

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("LogAnalyzer:LogFilePath", filePath)
                })
                .Build();

            var worker = new Worker(logger.Object, config);
            await worker.InvokeProcessLogsForTest();

            File.Delete(filePath);

            logger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error parsing log file lines")),
                    It.IsAny<LogFileParseException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task ProcessLogs_LogsCorrectMetricsAndAggregation()
        {
            var logger = new Mock<ILogger<Worker>>();
            var filePath = "test_metrics.log";
            File.WriteAllLines(filePath, new[]
            {
                "2025-07-10 14:30:21 INFO User login successful for user123",
                "2025-07-10 14:30:45 ERROR Database connection failed - timeout",
                "2025-07-10 14:31:02 WARNING High memory usage detected - 85%",
                "2025-07-10 14:31:15 INFO Order processed successfully - OrderId: 12345",
                "2025-07-10 14:31:30 ERROR Database connection failed - timeout",
                "2025-07-10 14:32:00 ERROR Payment processing failed for user456"
            });

            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("LogAnalyzer:LogFilePath", filePath)
                })
                .Build();

            var worker = new Worker(logger.Object, config);

            await worker.InvokeProcessLogsForTest();

            logger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("INFO: 2, WARNING: 1, ERROR: 3")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            logger.Verify(
                l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Most common ERROR: \"Database connection failed - timeout\" occurred 2 times")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);

            File.Delete(filePath);
        }
    }
}