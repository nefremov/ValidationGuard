namespace ValidationGuard.Http.Tests.Unit;

public class StatusPrecedenceTests
{
    [Fact]
    public void Default_RanksForbiddenNotFoundConflictUnprocessableBadRequest()
    {
        var precedence = StatusPrecedence.Default;

        Assert.True(precedence.Rank(403) > precedence.Rank(404));
        Assert.True(precedence.Rank(404) > precedence.Rank(409));
        Assert.True(precedence.Rank(409) > precedence.Rank(422));
        Assert.True(precedence.Rank(422) > precedence.Rank(400));
        Assert.True(precedence.Rank(400) > 0);
    }

    [Theory]
    [InlineData(410)]
    [InlineData(500)]
    [InlineData(200)]
    [InlineData(600)]
    [InlineData(-1)]
    public void Rank_UnlistedStatus_IsZero(int status)
    {
        Assert.Equal(0, StatusPrecedence.Default.Rank(status));
    }

    [Fact]
    public void Constructor_CustomOrder_RanksFirstHighest()
    {
        var precedence = new StatusPrecedence(400, 422);

        Assert.True(precedence.Rank(400) > precedence.Rank(422));
        Assert.Equal(0, precedence.Rank(403));
    }

    [Theory]
    [InlineData(399)]
    [InlineData(600)]
    public void Constructor_StatusOutsideErrorRange_Throws(int status)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new StatusPrecedence(status));
    }

    [Fact]
    public void Constructor_DuplicateStatus_Throws()
    {
        Assert.Throws<ArgumentException>(() => new StatusPrecedence(422, 400, 422));
    }
}
