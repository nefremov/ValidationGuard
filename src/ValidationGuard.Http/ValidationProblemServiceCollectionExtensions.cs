using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ValidationGuard.Http;

/// <summary>Registers the services that turn validation entries into Problem Details responses.</summary>
public static class ValidationProblemServiceCollectionExtensions
{
    /// <summary>
    /// Registers the error code map built by <paramref name="configure"/>, the default <see cref="StatusPrecedence"/>,
    /// the <see cref="ValidationProblemFactory"/>, and the <see cref="ValidationProblemExceptionHandler"/> for
    /// <see cref="ValidationFailedException"/>. Call it once, registering every code.
    /// </summary>
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
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IExceptionHandler, ValidationProblemExceptionHandler>());

        return services;
    }
}
