namespace ValidationGuard.Http.Tests.Unit;

public class StatusPrecedenceTests
{
    [Fact]
    public void Default_RanksForbiddenNotFoundConflictUnprocessableBadRequest()
    {
        // Arrange
        var precedence = StatusPrecedence.Default;

        // Act
        int[] ranks = [precedence.Rank(403), precedence.Rank(404), precedence.Rank(409), precedence.Rank(422), precedence.Rank(400)];

        // Assert
        Assert.Equal(ranks.OrderDescending(), ranks);
        Assert.Equal(ranks.Length, ranks.Distinct().Count());
        Assert.True(ranks[^1] > 0);
    }

    [Theory]
    [InlineData(410)]
    [InlineData(500)]
    [InlineData(200)]
    [InlineData(600)]
    [InlineData(-1)]
    public void Rank_UnlistedStatus_IsZero(int status)
    {
        // Act
        var rank = StatusPrecedence.Default.Rank(status);

        // Assert
        Assert.Equal(0, rank);
    }

    [Fact]
    public void Constructor_CustomOrder_RanksFirstHighest()
    {
        // Act
        var precedence = new StatusPrecedence(400, 422);

        // Assert
        Assert.True(precedence.Rank(400) > precedence.Rank(422));
        Assert.Equal(0, precedence.Rank(403));
    }

    [Theory]
    [InlineData(399)]
    [InlineData(600)]
    public void Constructor_StatusOutsideErrorRange_Throws(int status)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new StatusPrecedence(status));
    }

    [Fact]
    public void Constructor_DuplicateStatus_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new StatusPrecedence(422, 400, 422));
    }
}
