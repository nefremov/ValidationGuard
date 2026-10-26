namespace ValidationGuard.CommonErrors.Tests.Unit;

// The document validated by the common error and error definition tests.
public sealed class Order
{
    public static readonly Guid Id1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid Id2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid Id3 = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public Guid CustomerId { get; init; }

    public Guid OrderId { get; init; }

    public Guid LineId { get; init; }

    public string? Name { get; init; }

    public string? Email { get; init; }

    public string? Slug { get; init; }

    public string? Sku { get; init; }

    public Guid VendorId { get; init; }

    public int Age { get; init; }

    public int Quantity { get; init; }

    public decimal Price { get; init; }

    public decimal Weight { get; init; }

    public decimal? Discount { get; init; }

    public DateOnly DeliveryDate { get; init; }

    public Shipping Shipping { get; init; }

    public int ShippingCode { get; init; }

    public string? Country { get; init; }

    public Address Address { get; init; } = new();

    public List<Line> Lines { get; init; } = [];

    public Dictionary<string, string?> Attributes { get; init; } = new();
}

public sealed class Line
{
    public string? Sku { get; init; }
}

public sealed class Address
{
    public string? City { get; init; }

    public string? Street { get; init; }
}

public enum Shipping
{
    Standard,
    Express,
}

public enum Priority : long
{
    Low = 1,
    Huge = long.MaxValue,
}
