namespace ValidationGuard.Tests.Unit;

// @cpt-begin:cpt-validationguard-tests-validation-behavior:p1:inst-validation-behavior-guards
public class ValidationBehaviorTests
{
    [Fact]
    public void Constructor_WithNullConverter_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new ValidationBehavior(null!));

        Assert.Equal("nameConverter", exception.ParamName);
    }
}
// @cpt-end:cpt-validationguard-tests-validation-behavior:p1:inst-validation-behavior-guards
