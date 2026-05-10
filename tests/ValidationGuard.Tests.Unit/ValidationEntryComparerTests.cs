namespace ValidationGuard.Tests.Unit;

// @cpt-begin:cpt-validationguard-tests-validation-entry-comparer:p1:inst-deterministic-ordering
public class ValidationEntryComparerTests
{
    public static TheoryData<bool, (string Pointer, string Code, string Format, string Detail)?, (string Pointer, string Code, string Format, string Detail)?, int> CompareCases =>
        new()
        {
            { true, ("/a", "A1", "fmt", "detail"), ("/a", "A1", "fmt", "detail"), 0 },
            { false, null, ("/a", "A1", "fmt", "detail"), -1 },
            { false, ("/a", "A1", "fmt", "detail"), null, 1 },
            { false, ("/a", "Z1", "z", "z"), ("/b", "A1", "a", "a"), -1 },
            { false, ("/a", "A1", "z", "z"), ("/a", "B1", "a", "a"), -1 },
            { false, ("/a", "A1", "a", "z"), ("/a", "A1", "b", "a"), -1 },
            { false, ("/a", "A1", "a", "a"), ("/a", "A1", "a", "b"), -1 }
        };

    [Theory]
    [MemberData(nameof(CompareCases))]
    public void Compare_OrdersAsExpected(
        bool useSameInstance,
        (string Pointer, string Code, string Format, string Detail)? leftValues,
        (string Pointer, string Code, string Format, string Detail)? rightValues,
        int expectedSign)
    {
        ValidationEntry? left = leftValues is null
            ? null
            : new ValidationEntry(leftValues.Value.Pointer, leftValues.Value.Code, leftValues.Value.Format, leftValues.Value.Detail);

        ValidationEntry? right;
        if (useSameInstance)
        {
            right = left;
        }
        else
        {
            right = rightValues is null
                ? null
                : new ValidationEntry(rightValues.Value.Pointer, rightValues.Value.Code, rightValues.Value.Format, rightValues.Value.Detail);
        }

        var result = ValidationEntryComparer.Instance.Compare(left, right);

        Assert.Equal(expectedSign, Math.Sign(result));
    }
}
// @cpt-end:cpt-validationguard-tests-validation-entry-comparer:p1:inst-deterministic-ordering
