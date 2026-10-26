using static ValidationGuard.CommonErrors.Tests.Unit.TestHelpers;

namespace ValidationGuard.CommonErrors.Tests.Unit;

// AddTooShort and AddTooLong: they report the length, never the value itself.
public class LengthErrorTests
{
    [Fact]
    public void AddTooShort_RendersLengthAndMinimum()
    {
        // Act
        var entry = SingleEntry(v => v.AddTooShort("Order", x => x.Name, "Al", 3));

        // Assert
        Assert.Equal(CommonErrorCodes.TooShort, entry.Code);
        Assert.Equal("Value for name must be at least 3 characters long, but has 2", entry.Detail);
        MetadataJson.AssertEntries(entry, ("entity", "Order"), ("name", "name"), ("length", 2), ("min_length", 3));
    }

    [Fact]
    public void AddTooLong_RendersLengthAndMaximum()
    {
        // Act
        var entry = SingleEntry(v => v.AddTooLong("Order", x => x.Name, new string('x', 51), 50));

        // Assert
        Assert.Equal(CommonErrorCodes.TooLong, entry.Code);
        Assert.Equal("Value for name must be at most 50 characters long, but has 51", entry.Detail);
        MetadataJson.AssertEntries(entry, ("entity", "Order"), ("name", "name"), ("length", 51), ("max_length", 50));
    }

    [Fact]
    public void AddTooShort_NullValue_HasLengthZero()
    {
        // Act
        var entry = SingleEntry(v => v.AddTooShort("Order", x => x.Name, null, 1));

        // Assert
        Assert.Equal("Value for name must be at least 1 characters long, but has 0", entry.Detail);
    }

    [Fact]
    public void AddTooShort_ValueAppearsNeitherInDetailNorInMetadata()
    {
        // Arrange
        const string secret = "s3cr3t-token";

        // Act
        var entry = SingleEntry(v => v.AddTooShort("Order", x => x.Name, secret, 20));

        // Assert
        Assert.DoesNotContain(secret, entry.Detail);
        Assert.DoesNotContain(secret, MetadataJson.Of(entry).GetRawText());
    }

    [Fact]
    public void AddTooLong_NullValue_HasLengthZero()
    {
        // Act
        var entry = SingleEntry(v => v.AddTooLong("Order", x => x.Name, null, 0));

        // Assert
        Assert.Equal("Value for name must be at most 0 characters long, but has 0", entry.Detail);
    }
}
