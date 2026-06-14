using System.Net;

namespace Core.Exceptions;

public class CustomException : Exception
{
    /// <summary>
    /// List of error messages
    /// </summary>
    public IReadOnlyList<string> ErrorMessages { get; set; }

    /// <summary>
    /// Represents the HTTP status code associated with the exception.
    /// </summary>
    public HttpStatusCode StatusCode { get; set; }

    /// <summary>
    /// Represents a custom exception that includes additional error messages and an associated HTTP status code.
    /// </summary>
    public CustomException(string message, IEnumerable<string>? errors,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError)
        : base(message)
    {
        ErrorMessages = errors?.ToList() ?? new List<string>();
        StatusCode = statusCode;
    }

    /// <summary>
    /// Represents a custom exception that provides additional context through error messages
    /// and an associated HTTP status code.
    /// </summary>
    public CustomException(string message, Exception innerException,
        IEnumerable<string>? errors,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError) : base(message, innerException)
    {
        ErrorMessages = errors?.ToList() ?? new List<string>();
        StatusCode = statusCode;
    }

    /// <summary>
    /// Represents a custom exception that encapsulates error messages and an associated HTTP status code.
    /// </summary>
    public CustomException(
        string message,
        Exception innerException,
        HttpStatusCode statusCode) : this(message, innerException, Enumerable.Empty<string>(), statusCode)
    {
    }

    /// <summary>
    /// Represents a custom exception that includes error messages and an associated HTTP status code.
    /// </summary>
    public CustomException(string message)
        : this(message, Enumerable.Empty<string>(), HttpStatusCode.InternalServerError)
    {
    }
}