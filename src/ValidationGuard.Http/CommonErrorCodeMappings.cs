using ValidationGuard.CommonErrors;
using ValidationGuard.Http;

// HTTP statuses for ValidationGuard.CommonErrors. AddValidationProblems registers this assembly automatically;
// applications change a status with HttpErrorCodeOverride or ErrorCodeMapBuilder.Override.
[assembly: HttpErrorCode(CommonErrorCodes.NotFound, 404)]
[assembly: HttpErrorCode(CommonErrorCodes.AlreadyExists, 409)]
[assembly: HttpErrorCode(CommonErrorCodes.NotUnique, 409)]
[assembly: HttpErrorCode(CommonErrorCodes.Null, 422)]
[assembly: HttpErrorCode(CommonErrorCodes.Empty, 422)]
[assembly: HttpErrorCode(CommonErrorCodes.LessThan, 422)]
[assembly: HttpErrorCode(CommonErrorCodes.LessThanOrEqual, 422)]
[assembly: HttpErrorCode(CommonErrorCodes.GreaterThan, 422)]
[assembly: HttpErrorCode(CommonErrorCodes.GreaterThanOrEqual, 422)]
[assembly: HttpErrorCode(CommonErrorCodes.TooShort, 422)]
[assembly: HttpErrorCode(CommonErrorCodes.TooLong, 422)]
