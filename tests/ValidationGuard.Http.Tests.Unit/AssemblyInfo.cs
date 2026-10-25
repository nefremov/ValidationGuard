using ValidationGuard.Http;

[assembly: HttpErrorCode("TST001", 409, Type = "https://example.test/errors/TST001", Title = "Test conflict")]
[assembly: HttpErrorCode("TST002", 403)]
[assembly: HttpErrorCodeOverride("TST002", 404, Type = "https://example.test/errors/TST002")]
