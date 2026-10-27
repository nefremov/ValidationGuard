using Microsoft.AspNetCore.Http;

namespace ValidationGuard.Http;

/// <summary>
/// Which status a problem with entries of several statuses gets: the one ranked highest. Statuses not listed rank
/// below every listed one.
/// </summary>
public sealed class StatusPrecedence
{
    private const int MinStatus = 400;
    private const int StatusRange = 200;

    private readonly int[] _ranks = new int[StatusRange];

    /// <summary>Ranks the 4xx and 5xx statuses in <paramref name="mostDominantFirst"/>, the highest first.</summary>
    /// <exception cref="ArgumentOutOfRangeException">A status is not 4xx or 5xx.</exception>
    /// <exception cref="ArgumentException">A status is listed more than once.</exception>
    public StatusPrecedence(params ReadOnlySpan<int> mostDominantFirst)
    {
        for (var i = 0; i < mostDominantFirst.Length; i++)
        {
            var status = mostDominantFirst[i];
            var index = status - MinStatus;

            if ((uint)index >= StatusRange)
            {
                throw new ArgumentOutOfRangeException(nameof(mostDominantFirst), status, "Only 4xx and 5xx statuses can be ranked.");
            }

            if (_ranks[index] != 0)
            {
                throw new ArgumentException($"Status {status} is listed more than once.", nameof(mostDominantFirst));
            }

            _ranks[index] = mostDominantFirst.Length - i;
        }
    }

    /// <summary>403, then 404, 409, 422 and 400.</summary>
    public static StatusPrecedence Default { get; } = new(
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound,
        StatusCodes.Status409Conflict,
        StatusCodes.Status422UnprocessableEntity,
        StatusCodes.Status400BadRequest);

    /// <summary>The rank of <paramref name="status"/>: higher wins; 0 for a status not listed.</summary>
    public int Rank(int status)
    {
        var index = status - MinStatus;
        return (uint)index < StatusRange ? _ranks[index] : 0;
    }
}
