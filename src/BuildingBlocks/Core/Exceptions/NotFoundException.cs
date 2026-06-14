using System.Net;

namespace Core.Exceptions;

public class NotFoundException : CustomException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class with default message.
    /// </summary>
    public NotFoundException()
        : base("Resource not found.", Array.Empty<string>(), HttpStatusCode.NotFound)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NotFoundException"/> class with specified message.
    /// </summary>
    /// <param name="message">The error message describing what resource was not found.</param>
    public NotFoundException(string message)
        : base(message, Array.Empty<string>(), HttpStatusCode.NotFound)
    {
    }
}