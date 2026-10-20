namespace HuyetMach175.SharedKernel.Exceptions;

public class HttpException : Exception
{
    public int StatusCode { get; }

    public IDictionary<string, string>? Errors { get; }

    public HttpException(int statusCode, string message, IDictionary<string, string>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }
}