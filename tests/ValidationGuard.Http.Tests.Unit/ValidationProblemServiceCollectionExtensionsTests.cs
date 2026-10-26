using Microsoft.Extensions.DependencyInjection;

namespace ValidationGuard.Http.Tests.Unit;

public class ValidationProblemServiceCollectionExtensionsTests
{
    [Fact]
    public void AddValidationProblems_RegistersResolvableFactoryAndDefaultPrecedence()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddValidationProblems(
            codes => codes.Add("A001", 409).AddAssemblyOf<ValidationProblemServiceCollectionExtensionsTests>());
        using var provider = services.BuildServiceProvider();

        // Act
        var problem = provider.GetRequiredService<ValidationProblemFactory>()
            .Create([new ValidationEntry("/a", "TST002", "f", "Not found.")]);

        // Assert
        Assert.Equal(404, problem.Status);
        Assert.Same(StatusPrecedence.Default, provider.GetRequiredService<StatusPrecedence>());
    }

    [Fact]
    public void AddValidationProblems_CalledTwice_Throws()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddValidationProblems(codes => codes.Add("A001", 409));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => services.AddValidationProblems(codes => codes.Add("A002", 409)));
    }

    [Fact]
    public void AddValidationProblems_DuplicateRegistration_ThrowsAtRegistration()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => services.AddValidationProblems(codes => codes.Add("A001", 409).Add("A001", 422)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddValidationProblems_ExplicitPrecedence_IsUsedRegardlessOfOrder(bool registerBefore)
    {
        // Arrange
        var custom = new StatusPrecedence(400, 422);
        var services = new ServiceCollection().AddLogging();
        if (registerBefore)
        {
            services.AddSingleton(custom);
        }

        services.AddValidationProblems(codes => codes.Add("A400", 400).Add("A422", 422));
        if (!registerBefore)
        {
            services.AddSingleton(custom);
        }

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ValidationProblemFactory>();

        // Act
        var problem = factory.Create(
        [
            new ValidationEntry("/a", "A422", "f", "Unprocessable."),
            new ValidationEntry("/b", "A400", "f", "Bad."),
        ]);

        // Assert
        Assert.Equal(400, problem.Status);
    }

    [Fact]
    public void AddValidationProblems_NullArguments_Throw()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddValidationProblems(_ => { }));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddValidationProblems(null!));
    }
}
