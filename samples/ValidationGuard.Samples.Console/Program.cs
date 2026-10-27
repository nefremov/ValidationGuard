// Validates an order with the core builder, the common errors and the Acme extension, then prints every entry with
// its JSON pointer, code, rendered detail and metadata.
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Acme.Validation;
using ValidationGuard;
using ValidationGuard.CommonErrors;

var order = new Order
{
    CustomerId = Guid.Parse("3f2b8a51-0c7e-4d0b-9a55-6c1d2e3f40e1"),
    Email = "not-an-email",
    Total = 1234.5m,
    ShippingAddress = new Address { City = "Berlin", PostalCode = "" },
    BillingAddress = new Address { City = null, PostalCode = "10115-ABCDEFGHIJ" },
    Lines =
    [
        new Line { Sku = "CH-12", Quantity = 2 },
        new Line { Sku = null, Quantity = 0 },
    ],
    Tags = new() { ["gift/wrap"] = "" },
};

// The builder is disposable: once disposed, it rejects further entries.
using var validation = new ValidationBuilder<Order>(NameConverters.CamelCase);

// Common errors: stable codes, typed values, {name} filled with the property path.
validation.AddNotFound("Customer", x => x.CustomerId, order.CustomerId);

// Errors from the Acme extension library, added the same way.
validation.AddInvalidFormat("Order", x => x.Email, "^.+@.+$");
validation.AddInsufficientFunds(x => x.Total, order.Total, available: 100m, "EUR");

// Nested builders let a part of the document be validated by code that knows only that part. ValidateAddress works
// on an Address wherever it sits: the nested builder adds the scope, so the same rules report /shippingAddress/...
// for one call and /billingAddress/... for the other.
using (var shipping = validation.For(x => x.ShippingAddress))
{
    ValidateAddress(shipping, order.ShippingAddress);
}

if (order.BillingAddress is { } billingAddress)
{
    using var billing = validation.For(x => x.BillingAddress!);
    ValidateAddress(billing, billingAddress);
}

// The same for collection items: ValidateLine knows nothing about orders or indices; the scope adds /lines/{i}.
for (var i = 0; i < order.Lines.Count; i++)
{
    using var lineValidation = validation.For(x => x.Lines[i]);
    ValidateLine(lineValidation, order.Lines[i]);
}

// Dictionary keys are escaped in pointers ("~1") and quoted in readable paths.
foreach (var (key, value) in order.Tags)
{
    if (value.Length == 0)
    {
        validation.AddEmpty("Order", x => x.Tags[key]);
    }
}

// An application's own error: AddProperty creates the property metadata; the format may use {name}.
validation.AddProperty(x => x.Lines, "APP0001", "Value for {name} has more than one line for the same SKU");

// An entry about the document as a whole: no property, so it points at the builder's scope (the root here).
validation.Add("APP0002", "Orders are not accepted on Sundays");

foreach (var entry in validation.ToValidationEntries())
{
    Console.WriteLine($"{entry.Pointer,-30} {entry.Code,-9} {entry.Detail}");
    if (entry.Metadata is { } metadata)
    {
        Console.WriteLine($"{"",-40} metadata: {ToJson(metadata)}");
    }
}

// Rules for an Address, reusable for any address in any document: accessors start at the address.
static void ValidateAddress(IValidationBuilder<Address> validation, Address address)
{
    if (address.City is null)
    {
        validation.AddNull("Address", x => x.City);
    }

    if (string.IsNullOrEmpty(address.PostalCode))
    {
        validation.AddEmpty("Address", x => x.PostalCode);
    }
    else if (address.PostalCode.Length > 10)
    {
        validation.AddTooLong("Address", x => x.PostalCode, address.PostalCode, 10);
    }
}

// Rules for one order line; the caller's scope decides which line it is.
static void ValidateLine(IValidationBuilder<Line> validation, Line line)
{
    if (line.Sku is null)
    {
        validation.AddNull("OrderLine", x => x.Sku);
    }
    else if (line.Sku == "CH-12")
    {
        validation.AddSkuRetired(x => x.Sku, line.Sku);
    }

    if (line.Quantity is < 1 or > 100)
    {
        validation.AddNotBetween("OrderLine", x => x.Quantity, line.Quantity, 1, 100);
    }
}

static string ToJson(ValidationMetadata metadata)
{
    using var stream = new MemoryStream();
    // Relaxed escaping keeps the console output readable.
    using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
    {
        metadata.WriteTo(writer, JsonSerializerOptions.Web);
    }

    return Encoding.UTF8.GetString(stream.ToArray());
}

public sealed class Order
{
    public Guid CustomerId { get; init; }

    public string? Email { get; init; }

    public decimal Total { get; init; }

    public Address ShippingAddress { get; init; } = new();

    public Address? BillingAddress { get; init; }

    public List<Line> Lines { get; init; } = [];

    public Dictionary<string, string> Tags { get; init; } = [];
}

public sealed class Address
{
    public string? City { get; init; }

    public string? PostalCode { get; init; }
}

public sealed class Line
{
    public string? Sku { get; init; }

    public int Quantity { get; init; }
}
