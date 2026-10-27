using static ValidationGuard.CommonErrors.Tests.Unit.Order;

namespace ValidationGuard.CommonErrors.Tests.Unit;

// The catalog as a whole: codes, formats, and their metadata, plus argument checks shared by every helper.
public class CommonErrorCatalogTests
{
    [Fact]
    public void Codes_AreUniqueAndPrefixed()
    {
        // Act
        var codes = TestHelpers.Constants(typeof(CommonErrorCodes));

        // Assert
        Assert.All(codes, code => Assert.Matches("^VG[0-9]{4}$", code));
        Assert.Equal(codes.Count, codes.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void EveryFormatAndCode_IsUsed_AndEveryPlaceholderHasMatchingMetadata()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>(new ValidationOptions { NameConverter = NameConverters.CamelCase });
        validation.AddNotFound("E", x => x.OrderId, Id1);
        validation.AddNotFound("E", x => x.OrderId, Id1, x => x.LineId, Id2);
        validation.AddNotFound("E", x => x.OrderId, Id1, x => x.LineId, Id2, x => x.VendorId, Id3);
        validation.AddAlreadyExists("E", x => x.OrderId, Id1);
        validation.AddAlreadyExists("E", x => x.OrderId, Id1, x => x.LineId, Id2);
        validation.AddAlreadyExists("E", x => x.OrderId, Id1, x => x.LineId, Id2, x => x.VendorId, Id3);
        validation.AddNotUnique("E", x => x.Name, "n");
        validation.AddNotUnique("E", x => x.Name, "n", x => x.Quantity, 1);
        validation.AddNotUnique("E", x => x.Name, "n", x => x.Quantity, 1, x => x.Price, 2m);
        validation.AddNull("Order", x => x.Name);
        validation.AddEmpty("Order", x => x.Name);
        validation.AddLessThan("Order", x => x.Quantity, 0, 1);
        validation.AddLessThanOrEqual("Order", x => x.Quantity, 1, 1);
        validation.AddGreaterThan("Order", x => x.Quantity, 2, 1);
        validation.AddGreaterThanOrEqual("Order", x => x.Quantity, 1, 1);
        validation.AddTooShort("Order", x => x.Name, "", 1);
        validation.AddTooLong("Order", x => x.Name, "ab", 1);
        validation.AddOutOfRange("Order", x => x.Quantity, 0, 1, 10, RangeBounds.Inclusive);
        validation.AddOutOfRange("Order", x => x.Quantity, 1, 1, 10, RangeBounds.Exclusive);
        validation.AddOutOfRange("Order", x => x.Quantity, 10, 1, 10, RangeBounds.MinInclusive);
        validation.AddOutOfRange("Order", x => x.Quantity, 1, 1, 10, RangeBounds.MaxInclusive);
        validation.AddReferenceNotFound("Order", x => x.CustomerId, "Customer", Id1);
        validation.AddIncompatibleValues("Order", x => x.Shipping, Shipping.Express, x => x.Country, "CH");
        validation.AddNotAllowed("Order", x => x.Name, "x", ["a", "b"]);
        validation.AddNotAllowed("Order", x => x.ShippingCode, 7, typeof(Shipping));
        validation.AddNotModifiable("Order", x => x.OrderId);

        // Act
        var entries = validation.ToValidationEntries();

        // Assert
        Assert.Equal(
            TestHelpers.Constants(typeof(CommonErrorFormats)).ToHashSet(),
            entries.Select(e => e.Format).ToHashSet());
        Assert.Equal(
            TestHelpers.Constants(typeof(CommonErrorCodes)).ToHashSet(),
            entries.Select(e => e.Code).ToHashSet());
        Assert.All(entries, entry =>
        {
            // Every error carries its entity in metadata, whether or not the format shows it.
            Assert.Equal(
                ErrorDefinitionValidator.GetPlaceholders(entry.Format).Append("entity").ToHashSet(),
                MetadataJson.Keys(entry).ToHashSet());
            Assert.DoesNotContain("{", entry.Detail);
        });
    }

    [Fact]
    public void Helpers_WithNullBuilder_ThrowArgumentNullException()
    {
        // Arrange
        IValidationBuilder<Order> validation = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => validation.AddNotFound("Customer", x => x.CustomerId, Id1));
        Assert.Throws<ArgumentNullException>(() => validation.AddNull("Order", x => x.Name));
        Assert.Throws<ArgumentNullException>(() => validation.AddLessThan("Order", x => x.Quantity, 0, 1));
    }
}
