using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ValidationGuard.CommonErrors.Tests.Unit;

public class CommonErrorExtensionsTests
{
    private static readonly Guid Id1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Id2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Id3 = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Fact]
    public void AddNotFound_OneId()
    {
        var entry = Single(v => v.AddNotFound(x => x.CustomerId, "Customer", Id1));

        Assert.Equal("/customerId", entry.Pointer);
        Assert.Equal(CommonErrorCodes.NotFound, entry.Code);
        Assert.Equal("{entity} with {id} does not exist", entry.Format);
        Assert.Equal("Customer with 11111111-1111-1111-1111-111111111111 does not exist", entry.Detail);
        AssertMetadata(entry, ("entity", "Customer"), ("id", Id1));
    }

    [Fact]
    public void AddNotFound_TwoIds()
    {
        var entry = Single(v => v.AddNotFound(x => x.CustomerId, "OrderLine", Id1, Id2));

        Assert.Equal(CommonErrorCodes.NotFound, entry.Code);
        Assert.Equal("{entity} with {id1}, {id2} does not exist", entry.Format);
        Assert.Equal($"OrderLine with {Id1}, {Id2} does not exist", entry.Detail);
        AssertMetadata(entry, ("entity", "OrderLine"), ("id1", Id1), ("id2", Id2));
    }

    [Fact]
    public void AddNotFound_ThreeIds()
    {
        var entry = Single(v => v.AddNotFound(x => x.CustomerId, "Allocation", Id1, Id2, Id3));

        Assert.Equal(CommonErrorCodes.NotFound, entry.Code);
        Assert.Equal($"Allocation with {Id1}, {Id2}, {Id3} does not exist", entry.Detail);
        AssertMetadata(entry, ("entity", "Allocation"), ("id1", Id1), ("id2", Id2), ("id3", Id3));
    }

    [Fact]
    public void AddAlreadyExists_OneAndThreeIds()
    {
        var one = Single(v => v.AddAlreadyExists(x => x.CustomerId, "Customer", Id1));
        var three = Single(v => v.AddAlreadyExists(x => x.CustomerId, "Allocation", Id1, Id2, Id3));

        Assert.Equal(CommonErrorCodes.AlreadyExists, one.Code);
        Assert.Equal($"Customer with {Id1} already exists", one.Detail);
        Assert.Equal(CommonErrorCodes.AlreadyExists, three.Code);
        Assert.Equal("{entity} with {id1}, {id2}, {id3} already exists", three.Format);
        Assert.Equal($"Allocation with {Id1}, {Id2}, {Id3} already exists", three.Detail);
    }

    [Fact]
    public void EntityErrors_TupleIds_ProduceSameEntriesAsSeparateIds()
    {
        Assert.Equal(
            Single(v => v.AddNotFound(x => x.CustomerId, "OrderLine", Id1, Id2)),
            Single(v => v.AddNotFound(x => x.CustomerId, "OrderLine", (Id1, Id2))),
            EntryComparer.Instance);
        Assert.Equal(
            Single(v => v.AddNotFound(x => x.CustomerId, "Allocation", Id1, Id2, Id3)),
            Single(v => v.AddNotFound(x => x.CustomerId, "Allocation", (Id1, Id2, Id3))),
            EntryComparer.Instance);
        Assert.Equal(
            Single(v => v.AddAlreadyExists(x => x.CustomerId, "OrderLine", Id1, Id2)),
            Single(v => v.AddAlreadyExists(x => x.CustomerId, "OrderLine", (Id1, Id2))),
            EntryComparer.Instance);
        Assert.Equal(
            Single(v => v.AddAlreadyExists(x => x.CustomerId, "Allocation", Id1, Id2, Id3)),
            Single(v => v.AddAlreadyExists(x => x.CustomerId, "Allocation", (Id1, Id2, Id3))),
            EntryComparer.Instance);
    }

    [Fact]
    public void AddNotFound_CompositeKeyVariable_WithNamedElements()
    {
        (Guid OrderId, Guid LineId) key = (Id1, Id2);

        var entry = Single(v => v.AddNotFound(x => x.CustomerId, "OrderLine", key));

        Assert.Equal($"OrderLine with {Id1}, {Id2} does not exist", entry.Detail);
        AssertMetadata(entry, ("entity", "OrderLine"), ("id1", Id1), ("id2", Id2));
    }

    [Fact]
    public void AddNotUnique_OneProperty()
    {
        var entry = Single(v => v.AddNotUnique("User", x => x.Email, "ann@example.com"));

        Assert.Equal("/email", entry.Pointer);
        Assert.Equal(CommonErrorCodes.NotUnique, entry.Code);
        Assert.Equal("{entity} with {name} {value} already exists", entry.Format);
        Assert.Equal("User with email ann@example.com already exists", entry.Detail);
        AssertMetadata(entry, ("entity", "User"), ("name", "email"), ("value", "ann@example.com"));
    }

    [Fact]
    public void AddNotUnique_TwoProperties_PointsAtFirstProperty()
    {
        var entry = Single(
            v => v.AddNotUnique("Product", x => x.Sku, "CH-12", x => x.VendorId, Id1),
            ValidationBehavior.SnakeCase);

        Assert.Equal("/sku", entry.Pointer);
        Assert.Equal(CommonErrorCodes.NotUnique, entry.Code);
        Assert.Equal($"Product with sku CH-12 and vendor_id {Id1} already exists", entry.Detail);
        AssertMetadata(entry,
            ("entity", "Product"), ("name1", "sku"), ("value1", "CH-12"), ("name2", "vendor_id"), ("value2", Id1));
    }

    [Fact]
    public void AddNotUnique_ThreeProperties_MixedTypesAndNull()
    {
        var entry = Single(v => v.AddNotUnique(
            "Booking",
            x => x.Address.City, "Riga",
            x => x.DeliveryDate, new DateOnly(2026, 3, 1),
            x => x.Discount, (decimal?)null));

        Assert.Equal("/address/city", entry.Pointer);
        Assert.Equal(CommonErrorCodes.NotUnique, entry.Code);
        Assert.Equal("Booking with city Riga, deliveryDate 03/01/2026 and discount null already exists", entry.Detail);
        AssertMetadata(entry,
            ("entity", "Booking"),
            ("name1", "city"), ("value1", "Riga"),
            ("name2", "deliveryDate"), ("value2", new DateOnly(2026, 3, 1)),
            ("name3", "discount"), ("value3", null));
    }

    [Fact]
    public void AddNotUnique_AndAddAlreadyExists_UseDistinctCodes()
    {
        // Unique-constraint and primary-key conflicts read alike but carry different codes.
        var byProperty = Single(v => v.AddNotUnique("Customer", x => x.Name, "Acme"));
        var byId = Single(v => v.AddAlreadyExists(x => x.CustomerId, "Customer", Id1));

        Assert.Equal("Customer with name Acme already exists", byProperty.Detail);
        Assert.Equal($"Customer with {Id1} already exists", byId.Detail);
        Assert.Equal(CommonErrorCodes.NotUnique, byProperty.Code);
        Assert.Equal(CommonErrorCodes.AlreadyExists, byId.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddNotUnique_BlankEntity_Throws(string entity)
    {
        using var validation = ValidationBuilder<Order>.Create();

        Assert.ThrowsAny<ArgumentException>(() => validation.AddNotUnique(entity, x => x.Name, "x"));
    }

    [Fact]
    public void AddNull_NameFollowsNamingConvention()
    {
        var camel = Single(v => v.AddNull(x => x.CustomerId), ValidationBehavior.CamelCase);
        var snake = Single(v => v.AddNull(x => x.CustomerId), ValidationBehavior.SnakeCase);

        Assert.Equal(CommonErrorCodes.Null, camel.Code);
        Assert.Equal("Property {name} is null", camel.Format);
        Assert.Equal("Property customerId is null", camel.Detail);
        AssertMetadata(camel, ("name", "customerId"));
        Assert.Equal("Property customer_id is null", snake.Detail);
    }

    [Fact]
    public void AddNull_NestedPropertyAndNestedBuilder_UseLastSegment()
    {
        using var validation = ValidationBuilder<Order>.Create(ValidationBehavior.CamelCase);
        validation.AddNull(x => x.Address.City);
        using (var address = validation.For(x => x.Address))
        {
            address.AddNull(x => x.Street);
        }

        var entries = validation.ToValidationEntries();

        Assert.Contains(entries, e => e.Pointer == "/address/city" && e.Detail == "Property city is null");
        Assert.Contains(entries, e => e.Pointer == "/address/street" && e.Detail == "Property street is null");
    }

    [Fact]
    public void AddNull_KeyedIndexer_UnescapesPointerToken()
    {
        var entry = Single(v => v.AddNull(x => x.Attributes["a/b~c"]));

        Assert.Equal("/attributes/a~1b~0c", entry.Pointer);
        Assert.Equal("Property a/b~c is null", entry.Detail);
    }

    [Fact]
    public void AddEmpty()
    {
        var entry = Single(v => v.AddEmpty(x => x.Name));

        Assert.Equal(CommonErrorCodes.Empty, entry.Code);
        Assert.Equal("Property name is empty", entry.Detail);
    }

    [Fact]
    public void RangeErrors_RenderValueAndBoundAndExposeBothInMetadata()
    {
        var lessThan = Single(v => v.AddLessThan(x => x.Quantity, 0, 1));
        var lessOrEqual = Single(v => v.AddLessThanOrEqual(x => x.Quantity, 0, 0));
        var greaterThan = Single(v => v.AddGreaterThan(x => x.Quantity, 150, 100));
        var greaterOrEqual = Single(v => v.AddGreaterThanOrEqual(x => x.Quantity, 101, 101));

        Assert.Equal(
            (CommonErrorCodes.LessThan, "Property quantity is 0, which is less than 1"),
            (lessThan.Code, lessThan.Detail));
        Assert.Equal(
            (CommonErrorCodes.LessThanOrEqual, "Property quantity is 0, which is less than or equal to 0"),
            (lessOrEqual.Code, lessOrEqual.Detail));
        Assert.Equal(
            (CommonErrorCodes.GreaterThan, "Property quantity is 150, which is greater than 100"),
            (greaterThan.Code, greaterThan.Detail));
        Assert.Equal(
            (CommonErrorCodes.GreaterThanOrEqual, "Property quantity is 101, which is greater than or equal to 101"),
            (greaterOrEqual.Code, greaterOrEqual.Detail));
        AssertMetadata(lessThan, ("name", "quantity"), ("value", 0), ("min_value", 1));
        AssertMetadata(greaterThan, ("name", "quantity"), ("value", 150), ("max_value", 100));
    }

    [Fact]
    public void RangeErrors_NullablePropertyAndDateBound()
    {
        var discount = Single(v => v.AddGreaterThan(x => x.Discount, 0.75m, 0.5m));
        var missing = Single(v => v.AddLessThan(x => x.Discount, null, 0m));
        var date = Single(v => v.AddLessThan(x => x.DeliveryDate, new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)));

        Assert.Equal("Property discount is 0.75, which is greater than 0.5", discount.Detail);
        Assert.Equal("Property discount is null, which is less than 0", missing.Detail);
        Assert.Null(missing.Metadata!["value"]);
        Assert.Equal("Property deliveryDate is 01/01/2026, which is less than 01/31/2026", date.Detail);
    }

    [Fact]
    public void RangeErrors_FormatNumbersWithInvariantCulture()
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
        try
        {
            var entry = Single(v => v.AddLessThan(x => x.Price, 999.25m, 1234.5m));

            Assert.Equal("Property price is 999.25, which is less than 1234.5", entry.Detail);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Fact]
    public void LengthErrors_ReportLengthButNotTheValue()
    {
        var tooShort = Single(v => v.AddTooShort(x => x.Name, "Al", 3));
        var tooLong = Single(v => v.AddTooLong(x => x.Name, new string('x', 51), 50));
        var nullValue = Single(v => v.AddTooShort(x => x.Name, null, 1));

        Assert.Equal(CommonErrorCodes.TooShort, tooShort.Code);
        Assert.Equal("Property name is 2 characters long, which is shorter than 3", tooShort.Detail);
        AssertMetadata(tooShort, ("name", "name"), ("length", 2), ("min_length", 3));
        var secret = Single(v => v.AddTooShort(x => x.Name, "s3cr3t-token", 20));
        Assert.DoesNotContain("s3cr3t-token", secret.Detail);
        Assert.DoesNotContain("s3cr3t-token", secret.Metadata!.Values);
        Assert.Equal(CommonErrorCodes.TooLong, tooLong.Code);
        Assert.Equal("Property name is 51 characters long, which is longer than 50", tooLong.Detail);
        AssertMetadata(tooLong, ("name", "name"), ("length", 51), ("max_length", 50));
        Assert.Equal("Property name is 0 characters long, which is shorter than 1", nullValue.Detail);
    }

    [Fact]
    public void Render_DoesNotReplacePlaceholdersInsideArgumentValues()
    {
        var entry = Single(v => v.AddNotFound(x => x.CustomerId, "Order {id}", Id1));

        Assert.Equal($"Order {{id}} with {Id1} does not exist", entry.Detail);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void AddNotFound_BlankEntity_Throws(string entity)
    {
        using var validation = ValidationBuilder<Order>.Create();

        Assert.ThrowsAny<ArgumentException>(() => validation.AddNotFound(x => x.CustomerId, entity, Id1));
    }

    [Fact]
    public void NullBuilder_ThrowsArgumentNullException()
    {
        IValidationBuilder<Order> validation = null!;

        Assert.Throws<ArgumentNullException>(() => validation.AddNotFound(x => x.CustomerId, "Customer", Id1));
        Assert.Throws<ArgumentNullException>(() => validation.AddNull(x => x.Name));
        Assert.Throws<ArgumentNullException>(() => validation.AddLessThan(x => x.Quantity, 0, 1));
    }

    [Fact]
    public void Codes_AreUniqueAndPrefixed()
    {
        var codes = ConstantsOf(typeof(CommonErrorCodes));

        Assert.All(codes, code => Assert.Matches("^VG[0-9]{4}$", code));
        Assert.Equal(codes.Count, codes.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void EveryFormatPlaceholder_HasMatchingMetadata()
    {
        using var validation = ValidationBuilder<Order>.Create(ValidationBehavior.CamelCase);
        validation.AddNotFound(x => x.CustomerId, "E", Id1);
        validation.AddNotFound(x => x.CustomerId, "E", Id1, Id2);
        validation.AddNotFound(x => x.CustomerId, "E", Id1, Id2, Id3);
        validation.AddAlreadyExists(x => x.CustomerId, "E", Id1);
        validation.AddAlreadyExists(x => x.CustomerId, "E", Id1, Id2);
        validation.AddAlreadyExists(x => x.CustomerId, "E", Id1, Id2, Id3);
        validation.AddNotUnique("E", x => x.Name, "n");
        validation.AddNotUnique("E", x => x.Name, "n", x => x.Quantity, 1);
        validation.AddNotUnique("E", x => x.Name, "n", x => x.Quantity, 1, x => x.Price, 2m);
        validation.AddNull(x => x.Name);
        validation.AddEmpty(x => x.Name);
        validation.AddLessThan(x => x.Quantity, 0, 1);
        validation.AddLessThanOrEqual(x => x.Quantity, 1, 1);
        validation.AddGreaterThan(x => x.Quantity, 2, 1);
        validation.AddGreaterThanOrEqual(x => x.Quantity, 1, 1);
        validation.AddTooShort(x => x.Name, "", 1);
        validation.AddTooLong(x => x.Name, "ab", 1);

        var entries = validation.ToValidationEntries();

        // Every declared format and code is exercised above.
        Assert.Equal(
            ConstantsOf(typeof(CommonErrorFormats)).ToHashSet(),
            entries.Select(e => e.Format).ToHashSet());
        Assert.Equal(
            ConstantsOf(typeof(CommonErrorCodes)).ToHashSet(),
            entries.Select(e => e.Code).ToHashSet());

        foreach (var entry in entries)
        {
            var placeholders = Regex.Matches(entry.Format, "{([a-z0-9_]+)}").Select(m => m.Groups[1].Value);
            Assert.Equal(placeholders.ToHashSet(), entry.Metadata!.Keys.ToHashSet());
            Assert.DoesNotContain("{", entry.Detail);
        }
    }

    private static ValidationEntry Single(
        Action<IValidationBuilder<Order>> add,
        ValidationBehavior? behavior = null)
    {
        using var validation = ValidationBuilder<Order>.Create(behavior ?? ValidationBehavior.CamelCase);
        add(validation);
        return Assert.Single(validation.ToValidationEntries());
    }

    private static void AssertMetadata(ValidationEntry entry, params (string Key, object? Value)[] expected)
    {
        Assert.NotNull(entry.Metadata);
        Assert.Equal(expected.Select(e => e.Key), entry.Metadata.Keys);
        foreach (var (key, value) in expected)
        {
            Assert.Equal(value, entry.Metadata[key]);
        }
    }

    // ValidationEntry's record equality compares Metadata by reference; compare its contents instead.
    private sealed class EntryComparer : IEqualityComparer<ValidationEntry>
    {
        public static EntryComparer Instance { get; } = new();

        public bool Equals(ValidationEntry? x, ValidationEntry? y) =>
            x is not null && y is not null &&
            (x.Pointer, x.Code, x.Format, x.Detail) == (y.Pointer, y.Code, y.Format, y.Detail) &&
            x.Metadata!.SequenceEqual(y.Metadata!);

        public int GetHashCode(ValidationEntry obj) => HashCode.Combine(obj.Pointer, obj.Code, obj.Detail);
    }

    private static List<string> ConstantsOf(Type type) =>
        type.GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.IsLiteral)
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

    public sealed class Order
    {
        public Guid CustomerId { get; init; }

        public string? Name { get; init; }

        public string? Email { get; init; }

        public string? Sku { get; init; }

        public Guid VendorId { get; init; }

        public int Quantity { get; init; }

        public decimal Price { get; init; }

        public decimal? Discount { get; init; }

        public DateOnly DeliveryDate { get; init; }

        public Address Address { get; init; } = new();

        public Dictionary<string, string?> Attributes { get; init; } = new();
    }

    public sealed class Address
    {
        public string? City { get; init; }

        public string? Street { get; init; }
    }
}
