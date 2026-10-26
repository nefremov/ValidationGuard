namespace ValidationGuard.Http.Tests.Unit;

// Registering codes in code, the fallback status, and the builder's one-shot lifecycle.
public class ErrorCodeMapBuilderTests
{
    [Fact]
    public void Build_WithAddedCodes_ResolvesEachCode()
    {
        // Arrange
        var builder = new ErrorCodeMapBuilder()
            .Add("A001", 409, "https://example.test/errors/A001")
            .Add("A002", 422);

        // Act
        var map = builder.Build();

        // Assert
        Assert.Equal(2, map.Count);
        Assert.True(map.TryGetCode("A001", out var first));
        Assert.Equal(new HttpErrorCode("A001", 409, "https://example.test/errors/A001"), first);
        Assert.True(map.TryGetCode("A002", out var second));
        Assert.Equal(new HttpErrorCode("A002", 422), second);
        Assert.False(map.TryGetCode("A003", out _));
    }

    [Fact]
    public void Add_DuplicateCode_ThrowsSuggestingOverride()
    {
        // Arrange
        var builder = new ErrorCodeMapBuilder().Add("A001", 409);

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => builder.Add("A001", 422));

        // Assert
        Assert.Contains("'A001'", exception.Message);
        Assert.Contains("Override", exception.Message);
    }

    [Fact]
    public void Add_CodesDifferingOnlyInCase_AreDistinct()
    {
        // Arrange
        var builder = new ErrorCodeMapBuilder().Add("a001", 409).Add("A001", 422);

        // Act
        var map = builder.Build();

        // Assert
        Assert.Equal(2, map.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Add_EmptyCode_Throws(string? code)
    {
        // Arrange
        var builder = new ErrorCodeMapBuilder();

        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => builder.Add(code!, 409));
    }

    [Fact]
    public void UseFallbackStatus_SetsMapFallback()
    {
        // Arrange
        var builder = new ErrorCodeMapBuilder().UseFallbackStatus(400);

        // Act
        var map = builder.Build();

        // Assert
        Assert.Equal(400, map.FallbackStatus);
    }

    [Fact]
    public void Build_WithoutFallbackStatus_Uses422()
    {
        // Act
        var map = new ErrorCodeMapBuilder().Build();

        // Assert
        Assert.Equal(422, map.FallbackStatus);
    }

    [Fact]
    public void Builder_AfterBuild_RejectsChanges()
    {
        // Arrange
        var builder = new ErrorCodeMapBuilder();
        builder.Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => builder.Add("A001", 409));
        Assert.Throws<InvalidOperationException>(() => builder.Override("A001", 409));
        Assert.Throws<InvalidOperationException>(() => builder.UseFallbackStatus(400));
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }

    [Fact]
    public void Codes_ListsEveryRegisteredCode()
    {
        // Arrange
        var builder = new ErrorCodeMapBuilder().Add("A001", 409).Add("A002", 422);

        // Act
        var codes = builder.Build().Codes;

        // Assert
        Assert.Equal(["A001", "A002"], codes.Select(c => c.Code).Order());
    }
}
