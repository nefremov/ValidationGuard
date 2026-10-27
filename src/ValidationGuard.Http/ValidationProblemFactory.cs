using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ValidationGuard.Http;

/// <summary>
/// Turns validation entries into RFC 9457 Problem Details. The status is the one of the entry ranked highest by
/// <see cref="StatusPrecedence"/> (unregistered codes get <see cref="ErrorCodeMap.FallbackStatus"/>, with a logged
/// warning). When a single entry has that rank, its code's <c>type</c> and <c>title</c> and its <c>detail</c> describe
/// the problem; otherwise they are left to the status's defaults. Every entry is listed under <c>errors</c>.
/// </summary>
public sealed partial class ValidationProblemFactory
{
    /// <summary>The name of the Problem Details extension listing the entries.</summary>
    public const string ErrorsExtensionName = "errors";

    private readonly ErrorCodeMap _map;
    private readonly StatusPrecedence _precedence;
    private readonly ILogger _logger;

    /// <summary>Creates a factory over <paramref name="map"/> and <paramref name="precedence"/>.</summary>
    public ValidationProblemFactory(ErrorCodeMap map, StatusPrecedence precedence, ILogger<ValidationProblemFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(precedence);
        ArgumentNullException.ThrowIfNull(logger);

        _map = map;
        _precedence = precedence;
        _logger = logger;
    }

    /// <summary>Creates the Problem Details for <paramref name="entries"/>.</summary>
    /// <exception cref="ArgumentException"><paramref name="entries"/> is empty.</exception>
    public ProblemDetails Create(IReadOnlyList<ValidationEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var count = entries.Count;
        if (count == 0)
        {
            throw new ArgumentException("A validation problem requires at least one entry.", nameof(entries));
        }

        ValidationEntry? topEntry = null;
        HttpErrorCode? topCode = null;
        var topStatus = 0;
        var topRank = -1;
        var topCount = 0;

        for (var i = 0; i < count; i++)
        {
            var entry = entries[i];

            int status;
            if (_map.TryGetCode(entry.Code, out var errorCode))
            {
                status = errorCode.Status;
            }
            else
            {
                status = _map.FallbackStatus;
                LogUnmappedCode(_logger, entry.Code, status);
            }

            var rank = _precedence.Rank(status);
            if (rank > topRank)
            {
                topEntry = entry;
                topCode = errorCode;
                topStatus = status;
                topRank = rank;
                topCount = 1;
            }
            else if (rank == topRank)
            {
                topCount++;
            }
        }

        var problem = new ProblemDetails { Status = topStatus };

        if (topCount == 1)
        {
            problem.Type = topCode?.Type;
            problem.Title = topCode?.Title;
            problem.Detail = topEntry!.Detail;
        }

        problem.Extensions[ErrorsExtensionName] = new ValidationErrors(entries);

        return problem;
    }

    /// <summary>Creates a Minimal API result writing the Problem Details for <paramref name="entries"/>.</summary>
    public ProblemHttpResult CreateResult(IReadOnlyList<ValidationEntry> entries) => TypedResults.Problem(Create(entries));

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Validation code '{Code}' has no HTTP mapping; falling back to status {Status}.")]
    private static partial void LogUnmappedCode(ILogger logger, string code, int status);
}
