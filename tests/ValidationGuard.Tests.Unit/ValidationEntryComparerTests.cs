namespace ValidationGuard.Tests.Unit;

public class ValidationEntryComparerTests
{
    public static TheoryData<string[]?, string[]?, int> OrderCases =>
        new()
        {
            { null, ["/a", "A1", "fmt", "Detail"], -1 },
            { ["/a", "A1", "fmt", "Detail"], null, 1 },
            { ["/a", "A1", "fmt", "Detail"], ["/a", "A1", "fmt", "Detail"], 0 },
            { ["/a", "Z1", "z", "z"], ["/b", "A1", "a", "a"], -1 },
            { ["/a", "A1", "z", "z"], ["/a", "B1", "a", "a"], -1 },
            { ["/a", "A1", "a", "z"], ["/a", "A1", "b", "a"], -1 },
            { ["/a", "A1", "a", "a"], ["/a", "A1", "a", "b"], 0 },
        };

    [Theory]
    [MemberData(nameof(OrderCases))]
    public void Compare_OrdersByPointerCodeThenFormat_NotDetail_NullFirst(string[]? left, string[]? right, int expectedSign)
    {
        // Arrange
        var leftEntry = Entry(left);
        var rightEntry = Entry(right);

        // Act
        var result = ValidationEntryComparer.Instance.Compare(leftEntry, rightEntry);

        // Assert
        Assert.Equal(expectedSign, Math.Sign(result));
    }

    [Fact]
    public void Compare_SameInstance_ReturnsZero()
    {
        // Arrange
        var entry = new ValidationEntry("/a", "A1", "fmt", "Detail");

        // Act
        var result = ValidationEntryComparer.Instance.Compare(entry, entry);

        // Assert
        Assert.Equal(0, result);
    }

    private static ValidationEntry? Entry(string[]? values) =>
        values is null ? null : new ValidationEntry(values[0], values[1], values[2], values[3]);
}
