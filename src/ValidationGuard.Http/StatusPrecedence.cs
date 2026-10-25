using Microsoft.AspNetCore.Http;

namespace ValidationGuard.Http;

public sealed class StatusPrecedence
{
    private const int MinStatus = 400;
    private const int StatusRange = 200;

    private readonly int[] _ranks = new int[StatusRange];

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

    public static StatusPrecedence Default { get; } = new(
        StatusCodes.Status403Forbidden,
        StatusCodes.Status404NotFound,
        StatusCodes.Status409Conflict,
        StatusCodes.Status422UnprocessableEntity,
        StatusCodes.Status400BadRequest);

    public int Rank(int status)
    {
        var index = status - MinStatus;
        return (uint)index < StatusRange ? _ranks[index] : 0;
    }
}
