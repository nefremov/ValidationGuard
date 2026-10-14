using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ValidationGuard.Http;

public sealed partial class ValidationProblemFactory
{
    public const string ErrorsExtensionName = "errors";

    private readonly ErrorCodeMap _map;
    private readonly StatusPrecedence _precedence;
    private readonly ILogger _logger;

    public ValidationProblemFactory(ErrorCodeMap map, StatusPrecedence precedence, ILogger<ValidationProblemFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(precedence);
        ArgumentNullException.ThrowIfNull(logger);

        _map = map;
        _precedence = precedence;
        _logger = logger;
    }

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

    public ProblemHttpResult CreateResult(IReadOnlyList<ValidationEntry> entries) => TypedResults.Problem(Create(entries));

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Validation code '{Code}' has no HTTP mapping; falling back to status {Status}.")]
    private static partial void LogUnmappedCode(ILogger logger, string code, int status);
}
