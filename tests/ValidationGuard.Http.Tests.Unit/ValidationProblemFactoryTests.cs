using static ValidationGuard.Http.Tests.Unit.TestFactory;

namespace ValidationGuard.Http.Tests.Unit;

// ValidationProblemFactory.Create: choosing the status, type, title and detail from the entries.
public class ValidationProblemFactoryTests
{
    [Fact]
    public void Create_SingleEntry_UsesDeclaredTypeAndTitleAndEntryDetail()
    {
        // Arrange
        ValidationEntry[] entries = [Entry("/sku", "A409", "SKU 8812 is retired.")];

        // Act
        var problem = Instance.Create(entries);

        // Assert
        Assert.Equal(409, problem.Status);
        Assert.Equal(ConflictType, problem.Type);
        Assert.Equal(ConflictTitle, problem.Title);
        Assert.Equal("SKU 8812 is retired.", problem.Detail);
    }

    [Fact]
    public void Create_SingleTopRankedEntryAmongOthers_UsesItsDeclarationAndDetail()
    {
        // Arrange
        ValidationEntry[] entries =
        [
            Entry("/name", "A422", "Name is required."),
            Entry("/channel", "A403", "Channel 'web' is not permitted."),
            Entry("/qty", "A422", "Quantity is out of range."),
        ];

        // Act
        var problem = Instance.Create(entries);

        // Assert
        Assert.Equal(403, problem.Status);
        Assert.Equal(ForbiddenType, problem.Type);
        Assert.Equal(ForbiddenTitle, problem.Title);
        Assert.Equal("Channel 'web' is not permitted.", problem.Detail);
    }

    [Fact]
    public void Create_SingleTopRankedEntryWithoutDeclaredTypeOrTitle_LeavesThemForFrameworkDefaults()
    {
        // Arrange
        ValidationEntry[] entries = [Entry("/name", "A422", "Name is required.")];

        // Act
        var problem = Instance.Create(entries);

        // Assert
        Assert.Equal(422, problem.Status);
        Assert.Null(problem.Type);
        Assert.Null(problem.Title);
        Assert.Equal("Name is required.", problem.Detail);
    }

    [Fact]
    public void Create_SeveralEntriesOfTopRankedCode_LeavesGenericTypeTitleAndNoDetail()
    {
        // Arrange
        ValidationEntry[] entries =
        [
            Entry("/lines/0/sku", "A409", "SKU 8812 is retired."),
            Entry("/lines/1/sku", "A409", "SKU 9001 is retired."),
            Entry("/name", "A422", "Name is required."),
        ];

        // Act
        var problem = Instance.Create(entries);

        // Assert
        Assert.Equal(409, problem.Status);
        Assert.Null(problem.Type);
        Assert.Null(problem.Title);
        Assert.Null(problem.Detail);
    }

    [Fact]
    public void Create_DifferentCodesSharingTopStatus_LeavesGenericTypeAndTitle()
    {
        // Arrange
        ValidationEntry[] entries = [Entry("/a", "A409", "First."), Entry("/b", "B409", "Second.")];

        // Act
        var problem = Instance.Create(entries);

        // Assert
        Assert.Equal(409, problem.Status);
        Assert.Null(problem.Type);
        Assert.Null(problem.Title);
        Assert.Null(problem.Detail);
    }

    [Fact]
    public void Create_UnmappedCode_FallsBackTo422()
    {
        // Arrange
        ValidationEntry[] entries = [Entry("/x", "UNKNOWN", "Something is wrong.")];

        // Act
        var problem = Instance.Create(entries);

        // Assert
        Assert.Equal(422, problem.Status);
        Assert.Null(problem.Type);
        Assert.Null(problem.Title);
        Assert.Equal("Something is wrong.", problem.Detail);
    }

    [Fact]
    public void Create_UnmappedCodeAlongsideMappedFallbackStatus_CountsBothAsTopRanked()
    {
        // Arrange
        ValidationEntry[] entries = [Entry("/x", "UNKNOWN", "First."), Entry("/y", "A422", "Second.")];

        // Act
        var problem = Instance.Create(entries);

        // Assert
        Assert.Equal(422, problem.Status);
        Assert.Null(problem.Title);
        Assert.Null(problem.Detail);
    }

    [Fact]
    public void Create_AlwaysProjectsAllEntriesIntoErrorsExtension()
    {
        // Arrange
        ValidationEntry[] entries = [Entry("/a", "A409", "First."), Entry("/b", "A422", "Second.")];

        // Act
        var problem = Instance.Create(entries);

        // Assert
        var errors = Assert.IsType<ValidationErrors>(problem.Extensions[ValidationProblemFactory.ErrorsExtensionName]);
        Assert.Same(entries, errors.Entries);
    }

    [Fact]
    public void Create_NoEntries_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => Instance.Create([]));
    }

    [Fact]
    public void Create_UnmappedCode_LogsAWarningNamingCodeAndFallbackStatus()
    {
        // Arrange
        var logger = new CapturingLogger();
        var factory = new ValidationProblemFactory(
            new ErrorCodeMapBuilder().Add("A422", 422).Build(), StatusPrecedence.Default, logger);

        // Act
        factory.Create([Entry("/x", "UNKNOWN", "First."), Entry("/y", "A422", "Second.")]);

        // Assert
        var (level, message) = Assert.Single(logger.Entries);
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Warning, level);
        Assert.Contains("'UNKNOWN'", message);
        Assert.Contains("422", message);
    }

    [Fact]
    public void Create_NullEntries_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Instance.Create(null!));
    }

    [Fact]
    public void Constructor_NullArguments_Throw()
    {
        // Arrange
        var map = new ErrorCodeMapBuilder().Build();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<ValidationProblemFactory>.Instance;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ValidationProblemFactory(null!, StatusPrecedence.Default, logger));
        Assert.Throws<ArgumentNullException>(() => new ValidationProblemFactory(map, null!, logger));
        Assert.Throws<ArgumentNullException>(() => new ValidationProblemFactory(map, StatusPrecedence.Default, null!));
    }

    // Records each log call's level and formatted message.
    private sealed class CapturingLogger : Microsoft.Extensions.Logging.ILogger<ValidationProblemFactory>
    {
        public List<(Microsoft.Extensions.Logging.LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Entries.Add((logLevel, formatter(state, exception)));
    }
}
