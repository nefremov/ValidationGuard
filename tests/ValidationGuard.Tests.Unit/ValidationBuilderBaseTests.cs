namespace ValidationGuard.Tests.Unit;

// @cpt-begin:cpt-validationguard-tests-validation-builder-base:p1:inst-base-helpers-and-guards
public class ValidationBuilderBaseTests
{
    [Fact]
    public void Constructor_WithNullBehavior_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new ProbeBuilder<object>(null!));

        Assert.Equal("behavior", exception.ParamName);
    }

    [Fact]
    public void NormalizePointer_WithEmpty_ReturnsEmpty()
    {
        var value = ProbeBuilder<object>.Normalize(string.Empty);

        Assert.Equal(string.Empty, value);
    }

    [Fact]
    public void NormalizePointer_WithInvalidPrefix_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() => ProbeBuilder<object>.Normalize("invalid"));

        Assert.Equal("pointer", exception.ParamName);
    }

    [Fact]
    public void CombinePointers_WithLeftEmpty_ReturnsRight()
    {
        var combined = ProbeBuilder<object>.Combine(string.Empty, "/right");

        Assert.Equal("/right", combined);
    }

    [Fact]
    public void CombinePointers_WithRightEmpty_ReturnsLeft()
    {
        var combined = ProbeBuilder<object>.Combine("/left", string.Empty);

        Assert.Equal("/left", combined);
    }

    private sealed class ProbeBuilder<TContext> : ValidationBuilderBase<TContext>
    {
        public ProbeBuilder(ValidationBehavior behavior)
            : base(behavior)
        {
        }

        public static string Normalize(string pointer) => NormalizePointer(pointer);

        public static string Combine(string left, string right) => CombinePointers(left, right);

        protected override string ComposeChildPrefix(string localPrefix) => localPrefix;

        protected override string ComposeEntryPointer(string localPointer) => localPointer;

        protected override IValidationBuilder<TChild> CreateChild<TChild>(string prefix)
        {
            return new ProbeBuilder<TChild>(Behavior);
        }
    }
}
// @cpt-end:cpt-validationguard-tests-validation-builder-base:p1:inst-base-helpers-and-guards
