namespace ValidationGuard.Http.Tests.Unit;

public class ErrorCodeMapBuilderTests
{
    [Fact]
    public void Build_WithAddedCodes_ResolvesEachCode()
    {
        var map = new ErrorCodeMapBuilder()
            .Add("A001", 409, "https://example.test/errors/A001")
            .Add("A002", 422)
            .Build();

        Assert.Equal(2, map.Count);
        Assert.True(map.TryGetCode("A001", out var first));
        Assert.Equal(new HttpErrorCode("A001", 409, "https://example.test/errors/A001"), first);
        Assert.True(map.TryGetCode("A002", out var second));
        Assert.Equal(new HttpErrorCode("A002", 422), second);
        Assert.False(map.TryGetCode("A003", out _));
    }

    [Fact]
    public void Add_DuplicateCode_Throws()
    {
        var builder = new ErrorCodeMapBuilder().Add("A001", 409);

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Add("A001", 422));

        Assert.Contains("'A001'", exception.Message);
        Assert.Contains("Override", exception.Message);
    }

    [Fact]
    public void Add_CodeIsOrdinalCaseSensitive()
    {
        var map = new ErrorCodeMapBuilder().Add("a001", 409).Add("A001", 422).Build();

        Assert.Equal(2, map.Count);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Add_EmptyCode_Throws(string? code)
    {
        Assert.ThrowsAny<ArgumentException>(() => new ErrorCodeMapBuilder().Add(code!, 409));
    }

    [Fact]
    public void Override_RegisteredBeforeAdd_IsAppliedAtBuild()
    {
        var map = new ErrorCodeMapBuilder()
            .Override("A001", 422, "https://example.test/errors/override", "Overridden")
            .Add("A001", 409)
            .Build();

        Assert.True(map.TryGetCode("A001", out var code));
        Assert.Equal(new HttpErrorCode("A001", 422, "https://example.test/errors/override", "Overridden"), code);
    }

    [Fact]
    public void Build_OverrideWithoutRegisteredCode_Throws()
    {
        var builder = new ErrorCodeMapBuilder().Override("A001", 422);

        var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("'A001'", exception.Message);
    }

    [Fact]
    public void Override_SameCodeTwice_Throws()
    {
        var builder = new ErrorCodeMapBuilder().Override("A001", 422);

        Assert.Throws<InvalidOperationException>(() => builder.Override("A001", 400));
    }

    [Fact]
    public void AddAssembly_ReadsDeclarationsAndOverridesFromMetadata()
    {
        var map = new ErrorCodeMapBuilder().AddAssemblyOf<ErrorCodeMapBuilderTests>().Build();

        Assert.True(map.TryGetCode("TST001", out var declared));
        Assert.Equal(new HttpErrorCode("TST001", 409, "https://example.test/errors/TST001", "Test conflict"), declared);
        Assert.True(map.TryGetCode("TST002", out var overridden));
        Assert.Equal(new HttpErrorCode("TST002", 404, "https://example.test/errors/TST002"), overridden);
    }

    [Fact]
    public void AddAssembly_SameAssemblyTwice_ThrowsNamingTheAssembly()
    {
        var builder = new ErrorCodeMapBuilder().AddAssemblyOf<ErrorCodeMapBuilderTests>();

        var exception = Assert.Throws<InvalidOperationException>(() => builder.AddAssemblyOf<ErrorCodeMapBuilderTests>());

        Assert.Contains("ValidationGuard.Http.Tests.Unit", exception.Message);
    }

    [Fact]
    public void UseFallbackStatus_SetsMapFallback()
    {
        var map = new ErrorCodeMapBuilder().UseFallbackStatus(400).Build();

        Assert.Equal(400, map.FallbackStatus);
    }

    [Fact]
    public void Build_DefaultFallbackStatus_Is422()
    {
        Assert.Equal(422, new ErrorCodeMapBuilder().Build().FallbackStatus);
    }

    [Fact]
    public void Builder_AfterBuild_RejectsChanges()
    {
        var builder = new ErrorCodeMapBuilder();
        builder.Build();

        Assert.Throws<InvalidOperationException>(() => builder.Add("A001", 409));
        Assert.Throws<InvalidOperationException>(() => builder.Override("A001", 409));
        Assert.Throws<InvalidOperationException>(() => builder.UseFallbackStatus(400));
        Assert.Throws<InvalidOperationException>(() => builder.Build());
    }
}
