// A minimal API that validates an order and answers with RFC 9457 Problem Details: the status comes from the entries'
// codes, and every entry is listed under "errors" with its pointer, code, format, detail and metadata.
using Acme.Validation;
using Acme.Validation.Http;
using ValidationGuard;
using ValidationGuard.CommonErrors;
using ValidationGuard.CommonErrors.Http;
using ValidationGuard.Http;

[assembly: HttpErrorCode("APP0001", 422, Title = "Duplicate SKU")]

var builder = WebApplication.CreateBuilder(args);

// One call registers every code the application can report: the common errors, the Acme extension, and the
// application's own codes declared above. Any status can be overridden here.
builder.Services.AddValidationProblems(codes => codes
    .AddCommonErrors()
    .AddAcmeErrors()
    .AddAssemblyOf<Program>());

var app = builder.Build();

app.MapPost("/orders", (CreateOrder order, ValidationProblemFactory problems) =>
{
    using var validation = ValidationBuilder<CreateOrder>.Create(NameConverters.CamelCase);

    if (!Store.Customers.Contains(order.CustomerId))
    {
        validation.AddNotFound("Customer", x => x.CustomerId, order.CustomerId);
    }

    if (string.IsNullOrEmpty(order.Email))
    {
        validation.AddEmpty("Order", x => x.Email);
    }
    else if (!order.Email.Contains('@'))
    {
        validation.AddInvalidFormat("Order", x => x.Email, "^.+@.+$");
    }

    // Each line is validated by OrderLineValidator, which knows only OrderLine: the nested builder adds the
    // /lines/{i} scope to its pointers and property paths.
    for (var i = 0; i < order.Lines.Count; i++)
    {
        using var lineValidation = validation.For(x => x.Lines[i]);
        OrderLineValidator.Validate(lineValidation, order.Lines[i]);
    }

    if (order.Lines.GroupBy(line => line.Sku).Any(group => group.Count() > 1))
    {
        validation.AddProperty(x => x.Lines, "APP0001", "Value for {name} has more than one line for the same SKU");
    }

    var total = order.Lines.Sum(line => line.Quantity * Store.UnitPrice);
    if (total > Store.Balance)
    {
        validation.AddInsufficientFunds(x => x.Lines, total, Store.Balance, "EUR");
    }

    var entries = validation.ToValidationEntries();
    return entries.Count == 0
        ? Results.Created($"/orders/{Guid.NewGuid()}", order)
        : (IResult)problems.CreateResult(entries);
});

app.Run();

public sealed record CreateOrder(Guid CustomerId, string? Email, List<OrderLine> Lines);

public sealed record OrderLine(string? Sku, int Quantity);

// Rules for one order line, reusable wherever an OrderLine appears (an order, a cart, a quote): accessors start at the
// line, and the caller's builder decides where in the document it sits.
internal static class OrderLineValidator
{
    public static void Validate(IValidationBuilder<OrderLine> validation, OrderLine line)
    {
        if (string.IsNullOrEmpty(line.Sku))
        {
            validation.AddEmpty("OrderLine", x => x.Sku);
        }
        else if (line.Sku.Length > 12)
        {
            validation.AddTooLong("OrderLine", x => x.Sku, line.Sku, 12);
        }
        else if (Store.RetiredSkus.Contains(line.Sku))
        {
            validation.AddSkuRetired(x => x.Sku, line.Sku);
        }

        if (line.Quantity is < 1 or > 100)
        {
            validation.AddNotBetween("OrderLine", x => x.Quantity, line.Quantity, 1, 100);
        }
    }
}

internal static class Store
{
    public static readonly HashSet<Guid> Customers = [Guid.Parse("3f2b8a51-0c7e-4d0b-9a55-6c1d2e3f40e1")];

    public static readonly HashSet<string> RetiredSkus = ["CH-12"];

    public const decimal UnitPrice = 12.5m;

    public const decimal Balance = 500m;
}
