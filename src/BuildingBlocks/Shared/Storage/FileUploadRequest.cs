namespace Shared.Storage;

/// <summary>
/// Upload request carrying a raw stream — lifetime tied to the HTTP request.
/// Caller must NOT dispose the stream.
/// </summary>
public sealed class StreamUploadRequest
{
    public required string FileName    { get; init; }
    public required string ContentType { get; init; }
    public required Stream Stream      { get; init; }
}

/// <summary>
/// Upload request carrying buffered bytes — safe to use outside the HTTP request lifetime,
/// e.g. background jobs, Hangfire, message consumers.
/// </summary>
public sealed class BufferedUploadRequest
{
    public required string FileName    { get; init; }
    public required string ContentType { get; init; }
    public required byte[] Data        { get; init; }
}