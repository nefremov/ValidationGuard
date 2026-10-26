namespace ValidationGuard.Http.Tests.Unit;

// Registering the [HttpErrorCode] and [HttpErrorCodeOverride] declarations of an assembly (see AssemblyInfo.cs).
public class ErrorCodeAssemblyRegistrationTests
{
    [Fact]
    public void AddAssembly_ReadsDeclarationsAndOverridesFromMetadata()
    {
        // Arrange
        var builder = new ErrorCodeMapBuilder().AddAssemblyOf<ErrorCodeAssemblyRegistrationTests>();

        // Act
        var map = builder.Build();

        // Assert
        Assert.True(map.TryGetCode("TST001", out var declared));
        Assert.Equal(new HttpErrorCode("TST001", 409, "https://example.test/errors/TST001", "Test conflict"), declared);
        Assert.True(map.TryGetCode("TST002", out var overridden));
        Assert.Equal(new HttpErrorCode("TST002", 404, "https://example.test/errors/TST002"), overridden);
    }

    [Fact]
    public void AddAssembly_SameAssemblyTwice_ThrowsNamingTheAssembly()
    {
        // Arrange
        var builder = new ErrorCodeMapBuilder().AddAssemblyOf<ErrorCodeAssemblyRegistrationTests>();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(
            () => builder.AddAssemblyOf<ErrorCodeAssemblyRegistrationTests>());

        // Assert
        Assert.Contains("ValidationGuard.Http.Tests.Unit", exception.Message);
    }

    [Fact]
    public void AddAssembly_Null_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ErrorCodeMapBuilder().AddAssembly(null!));
    }
}
