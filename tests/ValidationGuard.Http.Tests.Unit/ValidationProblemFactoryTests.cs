using Microsoft.Extensions.Logging.Abstractions;

namespace ValidationGuard.Http.Tests.Unit;

public class ValidationProblemFactoryTests
{
    private const string ConflictType = "https://example.test/errors/A409";
    private const string ConflictTitle = "SKU retired";
    private const string ForbiddenType = "https://example.test/errors/A403";
    private const string ForbiddenTitle = "Channel not permitted";

    private static readonly ValidationProblemFactory Factory = new(
        new ErrorCodeMapBuilder()
            .Add("A409", 409, ConflictType, ConflictTitle)
            .Add("B409", 409)
            .Add("A403", 403, ForbiddenType, ForbiddenTitle)
            .Add("A422", 422)
            .Add("A400", 400)
            .Build(),
        StatusPrecedence.Default,
        NullLogger<ValidationProblemFactory>.Instance);

    [Fact]
    public void Create_SingleEntry_UsesDeclaredTypeAndTitleAndEntryDetail()
    {
        var problem = Factory.Create([Entry("/sku", "A409", "SKU 8812 is retired.")]);

        Assert.Equal(409, problem.Status);
        Assert.Equal(ConflictType, problem.Type);
        Assert.Equal(ConflictTitle, problem.Title);
        Assert.Equal("SKU 8812 is retired.", problem.Detail);
    }

    [Fact]
    public void Create_SingleTopRankedEntryAmongOthers_UsesItsDeclarationAndDetail()
    {
        var entries = new[]
        {
            Entry("/name", "A422", "Name is required."),
            Entry("/channel", "A403", "Channel 'web' is not permitted."),
            Entry("/qty", "A422", "Quantity is out of range."),
        };

        var problem = Factory.Create(entries);

        Assert.Equal(403, problem.Status);
        Assert.Equal(ForbiddenType, problem.Type);
        Assert.Equal(ForbiddenTitle, problem.Title);
        Assert.Equal("Channel 'web' is not permitted.", problem.Detail);
    }

    [Fact]
    public void Create_SingleTopRankedEntryWithoutDeclaredTypeOrTitle_LeavesThemForFrameworkDefaults()
    {
        var problem = Factory.Create([Entry("/name", "A422", "Name is required.")]);

        Assert.Equal(422, problem.Status);
        Assert.Null(problem.Type);
        Assert.Null(problem.Title);
        Assert.Equal("Name is required.", problem.Detail);
    }

    [Fact]
    public void Create_SeveralEntriesOfTopRankedCode_LeavesGenericTypeTitleAndNoDetail()
    {
        var entries = new[]
        {
            Entry("/lines/0/sku", "A409", "SKU 8812 is retired."),
            Entry("/lines/1/sku", "A409", "SKU 9001 is retired."),
            Entry("/name", "A422", "Name is required."),
        };

        var problem = Factory.Create(entries);

        Assert.Equal(409, problem.Status);
        Assert.Null(problem.Type);
        Assert.Null(problem.Title);
        Assert.Null(problem.Detail);
    }

    [Fact]
    public void Create_DifferentCodesSharingTopStatus_LeavesGenericTypeAndTitle()
    {
        var problem = Factory.Create([Entry("/a", "A409", "First."), Entry("/b", "B409", "Second.")]);

        Assert.Equal(409, problem.Status);
        Assert.Null(problem.Type);
        Assert.Null(problem.Title);
        Assert.Null(problem.Detail);
    }

    [Fact]
    public void Create_UnmappedCode_FallsBackTo422()
    {
        var problem = Factory.Create([Entry("/x", "UNKNOWN", "Something is wrong.")]);

        Assert.Equal(422, problem.Status);
        Assert.Null(problem.Type);
        Assert.Null(problem.Title);
        Assert.Equal("Something is wrong.", problem.Detail);
    }

    [Fact]
    public void Create_UnmappedCodeAlongsideMappedFallbackStatus_CountsBothAsTopRanked()
    {
        var problem = Factory.Create([Entry("/x", "UNKNOWN", "First."), Entry("/y", "A422", "Second.")]);

        Assert.Equal(422, problem.Status);
        Assert.Null(problem.Title);
        Assert.Null(problem.Detail);
    }

    [Fact]
    public void Create_AlwaysProjectsAllEntriesIntoErrorsExtension()
    {
        var entries = new[] { Entry("/a", "A409", "First."), Entry("/b", "A422", "Second.") };

        var problem = Factory.Create(entries);

        var errors = Assert.IsType<ValidationErrors>(problem.Extensions[ValidationProblemFactory.ErrorsExtensionName]);
        Assert.Same(entries, errors.Entries);
    }

    [Fact]
    public void Create_NoEntries_Throws()
    {
        Assert.Throws<ArgumentException>(() => Factory.Create([]));
    }

    [Fact]
    public void CreateResult_SeveralTopRankedEntries_FrameworkFillsGenericTypeAndTitle()
    {
        var result = Factory.CreateResult([Entry("/a", "A409", "First."), Entry("/b", "A409", "Second.")]);

        Assert.Equal(409, result.StatusCode);
        Assert.False(string.IsNullOrEmpty(result.ProblemDetails.Type));
        Assert.False(string.IsNullOrEmpty(result.ProblemDetails.Title));
        Assert.NotEqual(ConflictTitle, result.ProblemDetails.Title);
        Assert.Null(result.ProblemDetails.Detail);
    }

    [Fact]
    public void CreateResult_SingleEntry_KeepsDeclaredTypeAndTitle()
    {
        var result = Factory.CreateResult([Entry("/a", "A409", "SKU 8812 is retired.")]);

        Assert.Equal(ConflictType, result.ProblemDetails.Type);
        Assert.Equal(ConflictTitle, result.ProblemDetails.Title);
        Assert.Equal("SKU 8812 is retired.", result.ProblemDetails.Detail);
    }

    [Fact]
    public void CreateResult_SingleEntryWithoutDeclaredTitle_FrameworkFillsStableTitle()
    {
        var result = Factory.CreateResult([Entry("/name", "A422", "Name is required.")]);

        Assert.False(string.IsNullOrEmpty(result.ProblemDetails.Title));
        Assert.NotEqual("Name is required.", result.ProblemDetails.Title);
        Assert.Equal("Name is required.", result.ProblemDetails.Detail);
    }

    private static ValidationEntry Entry(string pointer, string code, string detail) =>
        new(pointer, code, detail, detail);
}
