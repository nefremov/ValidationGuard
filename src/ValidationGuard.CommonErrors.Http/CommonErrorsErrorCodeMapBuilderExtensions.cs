using ValidationGuard.Http;

namespace ValidationGuard.CommonErrors.Http;

/// <summary>Registers the HTTP statuses of the common errors.</summary>
public static class CommonErrorsErrorCodeMapBuilderExtensions
{
    /// <summary>
    /// Registers the HTTP statuses of the <see cref="CommonErrorCodes"/>: 404 for not found, 409 for already exists and
    /// not unique, 422 otherwise (including a reference to an entity that does not exist). Change one with <see cref="ErrorCodeMapBuilder.Override"/> or
    /// <see cref="HttpErrorCodeOverrideAttribute"/>.
    /// </summary>
    public static ErrorCodeMapBuilder AddCommonErrors(this ErrorCodeMapBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .Add(CommonErrorCodes.NotFound, 404)
            .Add(CommonErrorCodes.AlreadyExists, 409)
            .Add(CommonErrorCodes.NotUnique, 409)
            .Add(CommonErrorCodes.Null, 422)
            .Add(CommonErrorCodes.Empty, 422)
            .Add(CommonErrorCodes.LessThan, 422)
            .Add(CommonErrorCodes.LessThanOrEqual, 422)
            .Add(CommonErrorCodes.GreaterThan, 422)
            .Add(CommonErrorCodes.GreaterThanOrEqual, 422)
            .Add(CommonErrorCodes.TooShort, 422)
            .Add(CommonErrorCodes.TooLong, 422)
            .Add(CommonErrorCodes.OutOfRange, 422)
            .Add(CommonErrorCodes.ReferenceNotFound, 422)
            .Add(CommonErrorCodes.IncompatibleValues, 422)
            .Add(CommonErrorCodes.NotAllowedValue, 422)
            .Add(CommonErrorCodes.NotModifiable, 422);
    }
}
