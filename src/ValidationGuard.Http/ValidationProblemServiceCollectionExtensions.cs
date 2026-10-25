using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ValidationGuard.Http;

public static class ValidationProblemServiceCollectionExtensions
{
    public static IServiceCollection AddValidationProblems(
        this IServiceCollection services,
        Action<ErrorCodeMapBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        foreach (var descriptor in services)
        {
            if (descriptor.ServiceType == typeof(ErrorCodeMap))
            {
                throw new InvalidOperationException(
                    $"{nameof(AddValidationProblems)} has already been called. Register all error codes in a single call.");
            }
        }

        var builder = new ErrorCodeMapBuilder();
        configure(builder);

        services.AddSingleton(builder.Build());
        services.TryAddSingleton(StatusPrecedence.Default);
        services.TryAddSingleton<ValidationProblemFactory>();

        return services;
    }
}
