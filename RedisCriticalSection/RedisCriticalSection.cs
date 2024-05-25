using Microsoft.Extensions.Options;
using Polly;
using StackExchange.Redis;

namespace RedisCriticalSection;

public class RedisLock
{
    public RedisLock(IOptions<RedisCriticalSectionOption> options)
    {
        var redisConf = options.Value;
        var config = ConfigurationOptions.Parse($"{redisConf.Host}:{redisConf.Port}");
        if (!string.IsNullOrWhiteSpace(options.Value.Password))
        {
            config.Password = options.Value.Password;
        }

        lazyConnection = new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect(config));
    }

    private readonly Lazy<ConnectionMultiplexer> lazyConnection;

    private ConnectionMultiplexer Connection => lazyConnection.Value;

    public bool EnterCriticalSection(string lockKey, string clientId, TimeSpan expiry, TimeSpan retryInterval, int maxRetries, Action criticalSectionAction)
    {
        IDatabase redis = Connection.GetDatabase();

        Policy<bool> retryPolicy = Policy
            .HandleResult<bool>(result => !result)
            .WaitAndRetry(maxRetries, _ => retryInterval);

        bool enteredCriticalSection = retryPolicy.Execute(() =>
        {
            return redis.StringSet(lockKey, clientId, expiry, When.NotExists);
        });

        if (enteredCriticalSection)
        {
            try
            {
                // Execute the critical section action
                criticalSectionAction();
            }
            finally
            {
                // Exit the critical section
                ExitCriticalSection(lockKey, clientId);
            }
        }

        return enteredCriticalSection;
    }

    private void ExitCriticalSection(string lockKey, string clientId)
    {
        IDatabase redis = Connection.GetDatabase();
        RedisValue value = redis.StringGet(lockKey);
        if (value.HasValue && (string)value == clientId)
        {
            redis.KeyDelete(lockKey);
        }
    }
}