namespace ValidationGuard.Http.Tests.Unit;

// ErrorCodeMapBuilder.Override: replacing the mapping of a registered code.
public class ErrorCodeOverrideTests
{
    [Fact]
    public void Override_RegisteredBeforeAdd_IsAppliedAtBuild()
    {
        // Arrange
        var builder = new ErrorCodeMapBuilder()
            .Override("A001", 422, "https://example.test/errors/override", "Overridden")
            .Add("A001", 409);

        // Act
        var map = builder.Build();

        // Assert
        Assert.True(map.TryGetCode("A001", out var code));
        Assert.Equal(new HttpErrorCode("A001", 422, "https://example.test/errors/override", "Overridden"), code);
    }

    [Fact]
    public void Build_OverrideWithoutRegisteredCode_Throws()
    {
        // Arrange
        var builder = new ErrorCodeMapBuilder().Override("A001", 422);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        // Assert
        Assert.Contains("'A001'", exception.Message);
    }

    [Fact]
    public void Override_SameCodeTwice_Throws()
    {
        // Arrange
        var builder = new ErrorCodeMapBuilder().Override("A001", 422);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.Override("A001", 400));
    }
}
