namespace ValidationGuard.Tests.Unit;

public class ValidationBehaviorTests
{
    // @cpt-begin:cpt-validationguard-algo-validation-builder-convert-fragment-name:p1:inst-reject-missing-converter
    [Fact]
    public void Constructor_WithNullConverter_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new ValidationBehavior(null!));

        Assert.Equal("nameConverter", exception.ParamName);
    }
    // @cpt-end:cpt-validationguard-algo-validation-builder-convert-fragment-name:p1:inst-reject-missing-converter
}
