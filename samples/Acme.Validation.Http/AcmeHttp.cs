using Acme.Validation;
using ValidationGuard.Http;

[assembly: HttpErrorCode(AcmeErrorCodes.NotBetween, 422)]
[assembly: HttpErrorCode(AcmeErrorCodes.InvalidFormat, 422)]
[assembly: HttpErrorCode(
    AcmeErrorCodes.SkuRetired,
    409,
    Type = "https://docs.acme.example/errors/ACME0003",
    Title = "SKU retired")]
[assembly: HttpErrorCode(AcmeErrorCodes.InsufficientFunds, 422)]

namespace Acme.Validation.Http;

/// <summary>Registers the Acme statuses declared on this assembly.</summary>
public static class AcmeHttp
{
    public static ErrorCodeMapBuilder AddAcmeErrors(this ErrorCodeMapBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        return builder.AddAssembly(typeof(AcmeHttp).Assembly);
    }
}
