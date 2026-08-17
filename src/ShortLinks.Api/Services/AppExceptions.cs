namespace ShortLinks.Api.Services;

public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(int statusCode, string message) : base(message) => StatusCode = statusCode;
}

public sealed class AppValidationException(string message)
    : AppException(StatusCodes.Status400BadRequest, message);

public sealed class AppConflictException(string message)
    : AppException(StatusCodes.Status409Conflict, message);

public sealed class AppNotFoundException(string message)
    : AppException(StatusCodes.Status404NotFound, message);