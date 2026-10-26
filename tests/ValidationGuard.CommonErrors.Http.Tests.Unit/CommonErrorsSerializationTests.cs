using System.Text.Json;
using ValidationGuard.Http;

namespace ValidationGuard.CommonErrors.Http.Tests.Unit;

// Common errors end to end: typed metadata written as a JSON map in the "errors" extension.
public class CommonErrorsSerializationTests
{
    [Fact]
    public void Serialize_CommonErrors_TypedMetadataWritesItselfAsMap()
    {
        // Arrange
        var customerId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        using var validation = ValidationBuilder<Order>.Create(NameConverters.CamelCase);
        validation.AddNotFound("Customer", x => x.CustomerId, customerId);
        using (var line = validation.For(x => x.Lines[2]))
        {
            line.AddLessThan("OrderLine", x => x.Quantity, 0, 1);
            line.AddNotUnique("Line", x => x.Sku, "CH-12", x => x.Price, 9.5m);
        }

        var errors = new ValidationErrors(validation.ToValidationEntries());

        // Act
        var json = JsonSerializer.Serialize(errors, JsonSerializerOptions.Web);

        // Assert
        using var document = JsonDocument.Parse(json);
        var byPointer = document.RootElement.EnumerateArray()
            .ToDictionary(e => e.GetProperty("pointer").GetString()!, e => e.GetProperty("metadata").GetRawText());
        Assert.Equal(
            """{"entity":"Customer","name":"customerId","value":"11111111-1111-1111-1111-111111111111"}""",
            byPointer[""]);
        Assert.Equal(
            """{"entity":"OrderLine","name":"lines[2].quantity","value":0,"min_value":1}""",
            byPointer["/lines/2/quantity"]);
        Assert.Equal(
            """{"entity":"Line","name1":"lines[2].sku","value1":"CH-12","name2":"lines[2].price","value2":9.5}""",
            byPointer["/lines/2"]);
    }

    public sealed class Order
    {
        public Guid CustomerId { get; init; }

        public List<Line> Lines { get; init; } = [];
    }

    public sealed class Line
    {
        public int Quantity { get; init; }

        public string? Sku { get; init; }

        public decimal Price { get; init; }
    }
}
