using System.Net;

namespace Core.Exceptions;

public class UnauthorizedException : CustomException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with default message.
    /// </summary>
    public UnauthorizedException()
        : base("Authentication failed.", Array.Empty<string>(), HttpStatusCode.Unauthorized)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UnauthorizedException"/> class with specified message.
    /// </summary>
    /// <param name="message">The error message describing the authentication failure.</param>
    public UnauthorizedException(string message)
        : base(message, Array.Empty<string>(), HttpStatusCode.Unauthorized)
    {
    }
}