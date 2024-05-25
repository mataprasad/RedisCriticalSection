using RedisCriticalSection;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static void AddRedisCriticalSection(this IServiceCollection services,
        Action<RedisCriticalSectionOption> configure)
    {
        services.Configure(configure);
        services.AddSingleton<RedisLock>();
    }
}