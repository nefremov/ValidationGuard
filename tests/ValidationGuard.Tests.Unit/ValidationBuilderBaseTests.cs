namespace ValidationGuard.Tests.Unit;

public class ValidationBuilderBaseTests
{
    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act
        var exception = Assert.Throws<ArgumentNullException>(() => new ValidationBuilder<object>(null!));

        // Assert
        Assert.Equal("options", exception.ParamName);
    }
}
