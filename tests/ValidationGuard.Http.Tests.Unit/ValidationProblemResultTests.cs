using static ValidationGuard.Http.Tests.Unit.TestFactory;

namespace ValidationGuard.Http.Tests.Unit;

// ValidationProblemFactory.CreateResult: what ASP.NET Core fills in when the factory leaves type or title open.
public class ValidationProblemResultTests
{
    [Fact]
    public void CreateResult_SeveralTopRankedEntries_FrameworkFillsGenericTypeAndTitle()
    {
        // Arrange
        ValidationEntry[] entries = [Entry("/a", "A409", "First."), Entry("/b", "A409", "Second.")];

        // Act
        var result = Instance.CreateResult(entries);

        // Assert
        Assert.Equal(409, result.StatusCode);
        Assert.False(string.IsNullOrEmpty(result.ProblemDetails.Type));
        Assert.False(string.IsNullOrEmpty(result.ProblemDetails.Title));
        Assert.NotEqual(ConflictTitle, result.ProblemDetails.Title);
        Assert.Null(result.ProblemDetails.Detail);
    }

    [Fact]
    public void CreateResult_SingleEntry_KeepsDeclaredTypeAndTitle()
    {
        // Arrange
        ValidationEntry[] entries = [Entry("/a", "A409", "SKU 8812 is retired.")];

        // Act
        var result = Instance.CreateResult(entries);

        // Assert
        Assert.Equal(ConflictType, result.ProblemDetails.Type);
        Assert.Equal(ConflictTitle, result.ProblemDetails.Title);
        Assert.Equal("SKU 8812 is retired.", result.ProblemDetails.Detail);
    }

    [Fact]
    public void CreateResult_SingleEntryWithoutDeclaredTitle_FrameworkFillsStableTitle()
    {
        // Arrange
        ValidationEntry[] entries = [Entry("/name", "A422", "Name is required.")];

        // Act
        var result = Instance.CreateResult(entries);

        // Assert
        Assert.False(string.IsNullOrEmpty(result.ProblemDetails.Title));
        Assert.NotEqual("Name is required.", result.ProblemDetails.Title);
        Assert.Equal("Name is required.", result.ProblemDetails.Detail);
    }
}
