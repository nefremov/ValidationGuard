using Acme.Validation.Http;
using ValidationGuard.CommonErrors;
using ValidationGuard.Http;

namespace Acme.Validation.Tests;

// The checks an extending library needs for its catalog: valid definitions and an HTTP status for every code.
public class AcmeErrorsTests
{
    [Fact]
    public void Definitions_AreValid()
    {
        // Every placeholder has a key and every key a placeholder.

        // Act
        var plain = ErrorDefinitionValidator.ValidateFields(typeof(AcmeErrors));
        var generic = ErrorDefinitionValidator.ValidateFields(typeof(AcmeErrors.Range<int>));

        // Assert
        Assert.Equal((2, 1), (plain, generic));
    }

    [Fact]
    public void EveryCode_HasAnHttpStatus()
    {
        // Arrange
        var codes = typeof(AcmeErrorCodes).GetFields().Select(field => (string)field.GetRawConstantValue()!);

        // Act
        var map = new ErrorCodeMapBuilder().AddAcmeErrors().Build();

        // Assert
        Assert.All(codes, code => Assert.True(map.TryGetCode(code, out _), code));
    }
}
