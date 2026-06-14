using System.Net;

namespace Core.Exceptions;

public class ForbiddenException : CustomException
{
    public ForbiddenException()
        : base("Unauthorized access.", Array.Empty<string>(), HttpStatusCode.Forbidden)
    {
    }
    
    public ForbiddenException(string message)
        : base(message, Array.Empty<string>(), HttpStatusCode.Forbidden)
    {
    }
}