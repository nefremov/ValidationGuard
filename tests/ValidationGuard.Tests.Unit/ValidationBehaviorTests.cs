namespace ValidationGuard.Tests.Unit;

public class ValidationBehaviorTests
{
    [Fact]
    public void Constructor_WithNullConverter_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new ValidationBehavior(null!));

        Assert.Equal("nameConverter", exception.ParamName);
    }
}
