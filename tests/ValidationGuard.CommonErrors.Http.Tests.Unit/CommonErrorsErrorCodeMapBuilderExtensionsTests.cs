using Microsoft.Extensions.DependencyInjection;
using ValidationGuard.Http;

namespace ValidationGuard.CommonErrors.Http.Tests.Unit;

// AddCommonErrors: the HTTP status of every common error code.
public class CommonErrorsErrorCodeMapBuilderExtensionsTests
{
    public static TheoryData<string> CommonCodes => [.. typeof(CommonErrorCodes)
        .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
        .Select(f => (string)f.GetRawConstantValue()!)];

    [Theory]
    [MemberData(nameof(CommonCodes))]
    public void AddCommonErrors_MapsCode(string code)
    {
        // Arrange
        var expectedStatus = code switch
        {
            CommonErrorCodes.NotFound => 404,
            CommonErrorCodes.AlreadyExists or CommonErrorCodes.NotUnique => 409,
            _ => 422,
        };

        // Act
        var map = new ErrorCodeMapBuilder().AddCommonErrors().Build();

        // Assert
        Assert.True(map.TryGetCode(code, out var mapped), $"{code} is not mapped");
        Assert.Equal(expectedStatus, mapped.Status);
    }

    [Fact]
    public void AddValidationProblems_WithoutAddCommonErrors_DoesNotMapThem()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddValidationProblems(_ => { });
        using var provider = services.BuildServiceProvider();

        // Act
        var mapped = provider.GetRequiredService<ErrorCodeMap>().TryGetCode(CommonErrorCodes.NotFound, out _);

        // Assert
        Assert.False(mapped);
    }

    [Fact]
    public void AddCommonErrors_StatusCanBeOverridden()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddValidationProblems(codes => codes.AddCommonErrors().Override(CommonErrorCodes.NotFound, 422));
        using var provider = services.BuildServiceProvider();

        // Act
        var map = provider.GetRequiredService<ErrorCodeMap>();

        // Assert
        Assert.True(map.TryGetCode(CommonErrorCodes.NotFound, out var mapped));
        Assert.Equal(422, mapped.Status);
    }

    [Fact]
    public void AddCommonErrors_NullBuilder_Throws()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ((ErrorCodeMapBuilder)null!).AddCommonErrors());
    }
}
