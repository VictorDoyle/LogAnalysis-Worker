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

                //TODO: log analysis build out in task2
                await ProcessLogs();

                //config interval kicksin
                var intervalSeconds = _configuration.GetValue<int>("LogAnalyzer:IntervalSeconds", 30);
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
                foreach (var line in lines.Take(5))
                {
                    // null or empty lines = parsing error
                    if (string.IsNullOrWhiteSpace(line))
                        throw new FormatException("Log line is null or empty.");

                    _logger.LogInformation("[LOG]: {line}", line);
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