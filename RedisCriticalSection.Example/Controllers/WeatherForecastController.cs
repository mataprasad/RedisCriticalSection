using Microsoft.AspNetCore.Mvc;

namespace RedisCriticalSection.Example.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly RedisLock _redisLock;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, RedisLock redisLock)
    {
        _logger = logger;
        _redisLock = redisLock;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        
        string lockKey = "myCriticalSectionLock";
        string clientId = Guid.NewGuid().ToString();
        TimeSpan lockExpiry = TimeSpan.FromSeconds(30); // Lock expiration time
        TimeSpan retryInterval = TimeSpan.FromSeconds(1); // Retry interval
        int maxRetries = 3; // Maximum number of retry attempts

        // Attempt to enter the critical section with retry logic using Polly
        bool enteredCriticalSection = _redisLock.EnterCriticalSection(lockKey, clientId, lockExpiry, retryInterval, maxRetries, () =>
        {
            // Critical section code
            Console.WriteLine("Entered critical section");

            // Simulate some work
            Thread.Sleep(5000);

            Console.WriteLine("Exiting critical section");
        });

        if (!enteredCriticalSection)
        {
            Console.WriteLine("Failed to enter critical section after retries. Another process holds the lock.");
        }
        
        
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
            {
                Date = DateTime.Now.AddDays(index),
                TemperatureC = Random.Shared.Next(-20, 55),
                Summary = Summaries[Random.Shared.Next(Summaries.Length)]
            })
            .ToArray();
    }
}