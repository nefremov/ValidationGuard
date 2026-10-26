namespace ValidationGuard.Tests.Unit;

public class ValidationBuilderBaseTests
{
    [Fact]
    public void Constructor_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act
        var exception = Assert.Throws<ArgumentNullException>(() => new ProbeBuilder<object>(null!));

        // Assert
        Assert.Equal("options", exception.ParamName);
    }

    private sealed class ProbeBuilder<TContext> : ValidationBuilderBase<TContext>
    {
        public ProbeBuilder(ValidationOptions options)
            : base(options)
        {
        }
    }
}
