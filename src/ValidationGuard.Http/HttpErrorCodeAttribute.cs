namespace ValidationGuard.Http;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class HttpErrorCodeAttribute : Attribute
{
    public HttpErrorCodeAttribute(string code, int status)
    {
        Code = code;
        Status = status;
    }

    public string Code { get; }

    public int Status { get; }

    public string? Type { get; set; }

    public string? Title { get; set; }
}
