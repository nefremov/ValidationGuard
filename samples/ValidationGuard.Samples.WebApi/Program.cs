// A minimal API that validates an order and answers with RFC 9457 Problem Details: the status comes from the entries'
// codes, and every entry is listed under "errors" with its pointer, code, format, detail and metadata. Messages follow
// the request culture (?culture=de or Accept-Language: de): formats, entity and member names are translated, values are
// formatted for the culture, and pointers stay the same.
using Acme.Validation;
using Acme.Validation.Http;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using ValidationGuard;
using ValidationGuard.CommonErrors;
using ValidationGuard.CommonErrors.Http;
using ValidationGuard.Http;
using static ValidationGuard.Http.ValidationResults;

[assembly: HttpErrorCode("APP0001", 422, Title = "Duplicate SKU")]

var builder = WebApplication.CreateBuilder(args);

// One call registers every code the application can report: the common errors, the Acme extension, and the
// application's own codes declared above. Any status can be overridden here.
builder.Services.AddValidationProblems(codes => codes
    .AddCommonErrors()
    .AddAcmeErrors()
    .AddAssemblyOf<Program>());

// Optional: ASP.NET Core's Problem Details service. Its customizations apply to validation problems too: here a
// traceId, and the title translated for the request's culture (ValidationGuard localizes the detail and the errors,
// not the title). The exception handler middleware below also needs it to turn ValidationFailedException into a
// response.
builder.Services.AddProblemDetails(options =>
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;

        var titles = context.HttpContext.RequestServices.GetRequiredService<IStringLocalizer<AppMessages>>();
        if (context.ProblemDetails.Title is { } title && titles.Translate(title) is { } localized)
        {
            context.ProblemDetails.Title = localized;
        }
    });

// IStringLocalizer<T> over resources: the application's in Resources/, Acme's wherever Acme declares them.
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

// How every builder renders its entries, configured once with the options pattern: camelCase paths, and the
// localizers, asked in order (the application's first, so it can override Acme's translations).
builder.Services.AddOptions<ValidationOptions>()
    .Configure<IStringLocalizer<AppMessages>, IStringLocalizer<AcmeMessages>>((options, app, acme) =>
    {
        options.NameConverter = NameConverters.CamelCase;
        options.Localizers.Add(app);
        options.Localizers.Add(acme);
    });

builder.Services.AddSingleton<OrderService>();

var app = builder.Build();

// Sets CultureInfo.CurrentCulture (values) and CurrentUICulture (translations) per request from the query string, cookie
// or Accept-Language header; validation messages are rendered under them.
app.UseRequestLocalization(options => options
    .SetDefaultCulture("en")
    .AddSupportedCultures("en", "de")
    .AddSupportedUICultures("en", "de"));

// Turns ValidationFailedException, thrown by ThrowIfInvalid below the endpoints, into a Problem Details response.
app.UseExceptionHandler();

app.MapPost("/orders", (CreateOrder order, IOptions<ValidationOptions> options) =>
{
    using var validation = new ValidationBuilder<CreateOrder>(options.Value);

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

    // Problem(validation) produces the entries now and writes them as Problem Details when the response is sent, after
    // the builder is disposed.
    if (!validation.Valid)
    {
        return Problem(validation);
    }

    return Results.Created($"/orders/{Guid.NewGuid()}", order);
});

// Code below the endpoint that cannot return a result throws instead; the exception handler writes the same response.
app.MapPut("/orders/{id:guid}/email", (Guid id, ChangeEmail change, OrderService orders) =>
{
    orders.ChangeEmail(id, change);
    return Results.NoContent();
});

app.Run();

public sealed record CreateOrder(Guid CustomerId, string? Email, List<OrderLine> Lines);

public sealed record OrderLine(string? Sku, int Quantity);

public sealed record ChangeEmail(string? Email);

// A service below the endpoints gets the same options injected.
internal sealed class OrderService(IOptions<ValidationOptions> options)
{
    public void ChangeEmail(Guid orderId, ChangeEmail change)
    {
        using var validation = new ValidationBuilder<ChangeEmail>(options.Value);
        if (string.IsNullOrEmpty(change.Email))
        {
            validation.AddEmpty("Order", x => x.Email);
        }
        else if (!change.Email.Contains('@'))
        {
            validation.AddInvalidFormat("Order", x => x.Email, "^.+@.+$");
        }

        validation.ThrowIfInvalid();
        // ... change the order's email ...
    }
}

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

// The resources of the application's messages (Resources/AppMessages.de.resx), for IStringLocalizer<AppMessages>.
internal sealed class AppMessages;

internal static class Store
{
    public static readonly HashSet<Guid> Customers = [Guid.Parse("3f2b8a51-0c7e-4d0b-9a55-6c1d2e3f40e1")];

    public static readonly HashSet<string> RetiredSkus = ["CH-12"];

    public const decimal UnitPrice = 12.5m;

    public const decimal Balance = 500m;
}
