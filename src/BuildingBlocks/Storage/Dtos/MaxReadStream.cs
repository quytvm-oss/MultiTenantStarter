namespace Storage.Dtos;

internal sealed class MaxReadStream(Stream inner, long maxBytes) : Stream
{
    private long _totalRead;

    public override bool CanRead  => inner.CanRead;
    public override bool CanSeek  => false;
    public override bool CanWrite => false;
    public override long Length   => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        var read = inner.Read(buffer, offset, count);
        CheckLimit(read);
        return read;
    }

    public override int Read(Span<byte> buffer)
    {
        var read = inner.Read(buffer);
        CheckLimit(read);
        return read;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        var read = await inner.ReadAsync(buffer, offset, count, ct).ConfigureAwait(false);
        CheckLimit(read);
        return read;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        var read = await inner.ReadAsync(buffer, ct).ConfigureAwait(false);
        CheckLimit(read);
        return read;
    }

    private void CheckLimit(int read)
    {
        _totalRead += read;
        if (_totalRead > maxBytes)
            throw new InvalidOperationException("File exceeds the maximum allowed size.");
    }

    public override void Flush() { }
    public override long Seek(long offset, SeekOrigin origin)    => throw new NotSupportedException();
    public override void SetLength(long value)                    => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}