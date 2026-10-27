using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using static ValidationGuard.Http.ValidationResults;

namespace ValidationGuard.Http.Tests.Unit;

// ValidationResults.Problem and ValidationProblemResult: a handler returns Problem(validation) without resolving the
// factory itself.
public class ValidationResultsTests
{
    [Fact]
    public async Task Problem_WritesProblemDetailsWithStatusAndErrors()
    {
        // Arrange
        var result = Problem(BuilderWithEntry("A409", "SKU 8812 is retired."));
        var context = NewContext();

        // Act
        await result.ExecuteAsync(context);

        // Assert
        Assert.Equal(409, context.Response.StatusCode);
        Assert.StartsWith("application/problem+json", context.Response.ContentType);
        var body = ReadBody(context);
        Assert.Equal(409, body.GetProperty("status").GetInt32());
        Assert.Equal("SKU retired", body.GetProperty("title").GetString());
        Assert.Equal("SKU 8812 is retired.", body.GetProperty("detail").GetString());
        Assert.Equal("A409", body.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Problem_ProducesEntriesAtOnce_SoTheBuilderCanBeDisposedFirst()
    {
        // Arrange
        ValidationProblemResult result;
        using (var validation = new ValidationBuilder<Order>(NameConverters.CamelCase))
        {
            validation.AddProperty(x => x.Sku, "A409", "SKU is retired.");
            result = Problem(validation);
        }

        var context = NewContext();

        // Act
        await result.ExecuteAsync(context);

        // Assert
        Assert.Equal("/sku", Assert.Single(result.Entries).Pointer);
        Assert.Equal(409, context.Response.StatusCode);
    }

    [Fact]
    public async Task Problem_WithProblemDetailsService_AppliesItsCustomization()
    {
        // Arrange
        var result = Problem(BuilderWithEntry("A409", "SKU 8812 is retired."));
        var context = NewContext(services => services.AddProblemDetails(
            options => options.CustomizeProblemDetails = c => c.ProblemDetails.Extensions["traceId"] = "trace-1"));

        // Act
        await result.ExecuteAsync(context);

        // Assert
        var body = ReadBody(context);
        Assert.Equal("trace-1", body.GetProperty("traceId").GetString());
        Assert.Equal("A409", body.GetProperty("errors")[0].GetProperty("code").GetString());
    }

    [Fact]
    public async Task Problem_FromEntries_WritesTheSameResponse()
    {
        // Arrange
        var result = Problem([new ValidationEntry("/a", "A409", "f", "Retired.")]);
        var context = NewContext();

        // Act
        await result.ExecuteAsync(context);

        // Assert
        Assert.Equal(409, context.Response.StatusCode);
    }

    [Fact]
    public void Problem_WithoutEntries_Throws()
    {
        // Arrange
        using var validation = new ValidationBuilder<Order>();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Problem(validation));
        Assert.Throws<ArgumentException>(() => Problem([]));
        Assert.Throws<ArgumentNullException>(() => Problem((ValidationBuilder<Order>)null!));
        Assert.Throws<ArgumentNullException>(() => Problem((IReadOnlyList<ValidationEntry>)null!));
    }

    [Fact]
    public async Task ExecuteAsync_WithoutAddValidationProblems_Throws()
    {
        // Arrange
        var result = Problem(BuilderWithEntry("A409", "Retired."));
        var context = new DefaultHttpContext { RequestServices = new ServiceCollection().BuildServiceProvider() };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => result.ExecuteAsync(context));
        await Assert.ThrowsAsync<ArgumentNullException>(() => result.ExecuteAsync(null!));
    }

    [Fact]
    public void PopulateMetadata_DocumentsAProblemDetailsResponse()
    {
        // Arrange
        var builder = new TestEndpointBuilder();

        // Act
        ValidationProblemResult.PopulateMetadata(typeof(ValidationResultsTests).GetMethod(nameof(PopulateMetadata_DocumentsAProblemDetailsResponse))!, builder);

        // Assert
        var produces = Assert.IsAssignableFrom<IProducesResponseTypeMetadata>(Assert.Single(builder.Metadata));
        Assert.Equal((422, typeof(ProblemDetails)), (produces.StatusCode, produces.Type));
        Assert.Equal(["application/problem+json"], produces.ContentTypes);
        Assert.Throws<ArgumentNullException>(() => ValidationProblemResult.PopulateMetadata(null!, null!));
    }

    internal static ValidationBuilder<Order> BuilderWithEntry(string code, string detail)
    {
        var validation = new ValidationBuilder<Order>(NameConverters.CamelCase);
        validation.AddProperty(x => x.Sku, code, detail);
        return validation;
    }

    internal static DefaultHttpContext NewContext(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection().AddLogging();
        services.AddValidationProblems(codes => codes.Add("A409", 409, "https://example.test/errors/A409", "SKU retired"));
        configure?.Invoke(services);
        return new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider(),
            Response = { Body = new MemoryStream() },
        };
    }

    internal static JsonElement ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var document = JsonDocument.Parse(context.Response.Body);
        return document.RootElement.Clone();
    }

    public sealed class Order
    {
        public string? Sku { get; init; }
    }

    private sealed class TestEndpointBuilder : EndpointBuilder
    {
        public override Endpoint Build() => throw new NotSupportedException();
    }
}
