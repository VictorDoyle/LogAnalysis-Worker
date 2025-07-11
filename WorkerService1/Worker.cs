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

        if (string.IsNullOrEmpty(logFilePath) || !File.Exists(logFilePath))
        {
            _logger.LogWarning("Log file not found: {path}", logFilePath);
            return;
        }

        _logger.LogInformation("Processing log file: {path}", logFilePath);

        var lines = await File.ReadAllLinesAsync(logFilePath);
        _logger.LogInformation("Total lines in log file: {count}", lines.Length);

        //TODO:parsing logic to add here / onlyshowing 5 lines for now
        foreach (var line in lines.Take(5))
        {
            _logger.LogInformation("[LOG]: {line}", line);
        }
    }
}