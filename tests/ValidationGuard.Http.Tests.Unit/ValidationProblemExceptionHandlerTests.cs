using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using static ValidationGuard.Http.Tests.Unit.ValidationResultsTests;

namespace ValidationGuard.Http.Tests.Unit;

// ValidationProblemExceptionHandler: ThrowIfInvalid below the endpoint becomes the same Problem Details response.
public class ValidationProblemExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ValidationFailed_WritesTheProblemAndHandlesIt()
    {
        // Arrange
        using var validation = BuilderWithEntry("A409", "SKU 8812 is retired.");
        var exception = Assert.Throws<ValidationFailedException>(validation.ThrowIfInvalid);
        var context = NewContext();
        var handler = new ValidationProblemExceptionHandler();

        // Act
        var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

        // Assert
        Assert.True(handled);
        Assert.Equal(409, context.Response.StatusCode);
        Assert.Equal("/sku", ReadBody(context).GetProperty("errors")[0].GetProperty("pointer").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_OtherException_IsLeftToTheNextHandler()
    {
        // Arrange
        var context = NewContext();
        var handler = new ValidationProblemExceptionHandler();

        // Act
        var handled = await handler.TryHandleAsync(context, new InvalidOperationException(), CancellationToken.None);

        // Assert
        Assert.False(handled);
        Assert.Equal(0, context.Response.Body.Length);
    }

    [Fact]
    public async Task TryHandleAsync_NullContext_Throws()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await new ValidationProblemExceptionHandler().TryHandleAsync(null!, new Exception(), CancellationToken.None));
    }

    [Fact]
    public void AddValidationProblems_RegistersTheHandlerOnce()
    {
        // Arrange
        var services = new ServiceCollection().AddLogging();
        services.AddValidationProblems(_ => { });

        // Act
        var handlers = services.BuildServiceProvider().GetServices<IExceptionHandler>();

        // Assert
        Assert.IsType<ValidationProblemExceptionHandler>(Assert.Single(handlers));
    }
}
