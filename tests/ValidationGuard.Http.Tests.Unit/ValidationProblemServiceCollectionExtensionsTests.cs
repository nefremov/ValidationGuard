using Microsoft.Extensions.DependencyInjection;

namespace ValidationGuard.Http.Tests.Unit;

public class ValidationProblemServiceCollectionExtensionsTests
{
    [Fact]
    public void AddValidationProblems_RegistersResolvableFactory()
    {
        var services = new ServiceCollection().AddLogging();
        services.AddValidationProblems(codes => codes.Add("A001", 409).AddAssemblyOf<ErrorCodeMapBuilderTests>());

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<ValidationProblemFactory>();

        var problem = factory.Create([new ValidationEntry("/a", "TST002", "f", "Not found.")]);

        Assert.Equal(404, problem.Status);
        Assert.Same(StatusPrecedence.Default, provider.GetRequiredService<StatusPrecedence>());
    }

    [Fact]
    public void AddValidationProblems_MapsEveryCommonErrorCodeByDefault()
    {
        var services = new ServiceCollection();
        services.AddValidationProblems(_ => { });

        using var provider = services.BuildServiceProvider();
        var map = provider.GetRequiredService<ErrorCodeMap>();

        var commonCodes = typeof(CommonErrors.CommonErrorCodes)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(f => (string)f.GetRawConstantValue()!);

        foreach (var code in commonCodes)
        {
            Assert.True(map.TryGetCode(code, out var mapped), $"{code} is not mapped");
            var expected = code switch
            {
                CommonErrors.CommonErrorCodes.NotFound => 404,
                CommonErrors.CommonErrorCodes.AlreadyExists => 409,
                CommonErrors.CommonErrorCodes.NotUnique => 409,
                _ => 422,
            };
            Assert.Equal(expected, mapped.Status);
        }
    }

    [Fact]
    public void AddValidationProblems_CommonErrorStatus_CanBeOverridden()
    {
        var services = new ServiceCollection();
        services.AddValidationProblems(codes => codes.Override(CommonErrors.CommonErrorCodes.NotFound, 422));

        using var provider = services.BuildServiceProvider();
        var map = provider.GetRequiredService<ErrorCodeMap>();

        Assert.True(map.TryGetCode(CommonErrors.CommonErrorCodes.NotFound, out var mapped));
        Assert.Equal(422, mapped.Status);
    }

    [Fact]
    public void AddValidationProblems_CalledTwice_Throws()
    {
        var services = new ServiceCollection();
        services.AddValidationProblems(codes => codes.Add("A001", 409));

        Assert.Throws<InvalidOperationException>(() => services.AddValidationProblems(codes => codes.Add("A002", 409)));
    }

    [Fact]
    public void AddValidationProblems_DuplicateRegistration_ThrowsAtRegistration()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() =>
            services.AddValidationProblems(codes => codes.Add("A001", 409).Add("A001", 422)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddValidationProblems_ExplicitPrecedence_IsUsedRegardlessOfOrder(bool registerBefore)
    {
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
        var problem = provider.GetRequiredService<ValidationProblemFactory>().Create(
        [
            new ValidationEntry("/a", "A422", "f", "Unprocessable."),
            new ValidationEntry("/b", "A400", "f", "Bad."),
        ]);

        Assert.Equal(400, problem.Status);
    }
}
