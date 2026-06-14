namespace Storage.Dtos;

/// <summary>
/// Wraps the S3 response stream and disposes the owning <see cref="GetObjectResponse"/>
/// when the stream itself is disposed, preventing connection leaks.
/// </summary>
public sealed class ResponseStream : Stream
{
    private readonly Stream _inner;
    private readonly object _owner;
    private int _disposed;

    public ResponseStream(Stream inner, object owner)
    {
        ArgumentNullException.ThrowIfNull(inner);
        ArgumentNullException.ThrowIfNull(owner);
        if (owner is not IDisposable && owner is not IAsyncDisposable)
            throw new ArgumentException("Owner must implement IDisposable or IAsyncDisposable.", nameof(owner));
        _inner = inner;
        _owner = owner;
    }

    public override bool CanRead  => !IsDisposed && _inner.CanRead;
    public override bool CanSeek  => !IsDisposed && _inner.CanSeek;
    public override bool CanWrite => !IsDisposed && _inner.CanWrite;

    public override long Length
    {
        get { ThrowIfDisposed(); return _inner.Length; }
    }

    public override long Position
    {
        get { ThrowIfDisposed(); return _inner.Position; }
        set { ThrowIfDisposed(); _inner.Position = value; }
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        return _inner.Read(buffer, offset, count);
    }

    public override int Read(Span<byte> buffer)
    {
        ThrowIfDisposed();
        return _inner.Read(buffer);
    }

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        ThrowIfDisposed();
        return _inner.ReadAsync(buffer, offset, count, ct);
    }

    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return _inner.ReadAsync(buffer, ct);
    }

    public override void Flush()
    {
        ThrowIfDisposed();
        _inner.Flush();
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        return _inner.FlushAsync(cancellationToken);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        ThrowIfDisposed();
        return _inner.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        ThrowIfDisposed();
        _inner.SetLength(value);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ThrowIfDisposed();
        _inner.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ThrowIfDisposed();
        _inner.Write(buffer);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct)
    {
        ThrowIfDisposed();
        return _inner.WriteAsync(buffer, offset, count, ct);
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
    {
        ThrowIfDisposed();
        return _inner.WriteAsync(buffer, ct);
    }

    protected override void Dispose(bool disposing)
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0 && disposing)
        {
            _inner.Dispose();
            if (_owner is IDisposable disposable)
                disposable.Dispose();
            else if (_owner is IAsyncDisposable asyncDisposable)
                asyncDisposable.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            await _inner.DisposeAsync().ConfigureAwait(false);
            if (_owner is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync().ConfigureAwait(false);
            else if (_owner is IDisposable disposable)
                disposable.Dispose();
        }
        await base.DisposeAsync().ConfigureAwait(false);
    }

    private bool IsDisposed => Volatile.Read(ref _disposed) == 1;

    private void ThrowIfDisposed()
        => ObjectDisposedException.ThrowIf(IsDisposed, this);
}