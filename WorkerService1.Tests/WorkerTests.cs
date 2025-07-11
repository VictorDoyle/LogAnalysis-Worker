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
            File.WriteAllLines(filePath, new string[] { null, "valid line" });

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
    }
}