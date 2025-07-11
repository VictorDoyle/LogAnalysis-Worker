using Microsoft.Extensions.Options;

namespace WorkerService1;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

#if DEBUG
    public async Task InvokeProcessLogsForTest() => await ProcessLogs();
#endif

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Log analyzer running at: {time}", DateTimeOffset.Now);

                await ProcessLogs();

                //config interval kicksin
                var intervalSeconds = _configuration.GetValue<int>("LogAnalyzer:IntervalSeconds", 3);
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in log analyzer");
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); //wait before retryging again
            }
        }
    }

    private async Task ProcessLogs()
    {
        var logFilePath = _configuration.GetValue<string>("LogAnalyzer:LogFilePath");

        try
        {
            if (string.IsNullOrEmpty(logFilePath))
            {
                throw new LogFileNotFoundException("Path is null or empty.");
            }

            if (!File.Exists(logFilePath))
            {
                throw new LogFileNotFoundException(logFilePath);
            }

            _logger.LogInformation("Processing log file: {path}", logFilePath);

            string[] lines;
            try
            {
                lines = await File.ReadAllLinesAsync(logFilePath);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new LogFileReadException(logFilePath, ex);
            }
            catch (IOException ex)
            {
                throw new LogFileReadException(logFilePath, ex);
            }
            catch (Exception ex)
            {
                throw new LogFileReadException(logFilePath, ex);
            }

            _logger.LogInformation("Total lines in log file: {count}", lines.Length);

            try
            {
                var entries = new List<LogEntry>();
                foreach (var line in lines)
                {
                    var entry = LogParser.Parse(line);
                    if (entry == null)
                        throw new FormatException("Error parsing log file lines.");
                    entries.Add(entry);
                }

                //count by level
                var infoCount = entries.Count(e => e.Level == "INFO");
                var warningCount = entries.Count(e => e.Level == "WARNING");
                var errorCount = entries.Count(e => e.Level == "ERROR");

                _logger.LogInformation("INFO: {info}, WARNING: {warn}, ERROR: {error}", infoCount, warningCount, errorCount);

                // aggr most common error message
                var mostCommonError = entries
                    .Where(e => e.Level == "ERROR")
                    .GroupBy(e => e.Message)
                    .OrderByDescending(g => g.Count())
                    .FirstOrDefault();

                if (mostCommonError != null)
                {
                    _logger.LogInformation("Most common ERROR: \"{msg}\" occurred {count} times", mostCommonError.Key, mostCommonError.Count());
                }

                foreach (var entry in entries.Take(5))
                {
                    _logger.LogInformation("[{level}] {time}: {msg}", entry.Level, entry.Timestamp, entry.Message);
                }
            }

            catch (FormatException ex)
            {
                throw new LogFileParseException("Error parsing log file lines.", ex);
            }
            catch (Exception ex)
            {
                throw new LogFileParseException("Unknown error during log parsing.", ex);
            }
        }
        catch (LogFileNotFoundException ex)
        {
            _logger.LogWarning(ex, ex.Message);
        }
        catch (LogFileReadException ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        catch (LogFileParseException ex)
        {
            _logger.LogError(ex, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error in ProcessLogs");
        }
    }
}