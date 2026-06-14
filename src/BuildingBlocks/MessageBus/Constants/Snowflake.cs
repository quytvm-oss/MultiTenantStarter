namespace MessageBus.Constants;

public static class Snowflake
{
    // Epoch tùy chỉnh
    private static readonly DateTime Epoch =
        new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // 41 bits timestamp
    // 10 bits workerId
    // 12 bits sequence

    private const int WorkerIdBits = 10;
    private const int SequenceBits = 12;

    private const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);
    private const long MaxSequence = -1L ^ (-1L << SequenceBits);

    private static readonly object Lock = new();

    private static long _lastTimestamp = -1L;
    private static long _sequence = 0;

    // đổi theo instance/server
    private static long _workerId;

    public static void Configure(long workerId)
    {
        if (workerId < 0 || workerId > MaxWorkerId)
        {
            throw new ArgumentOutOfRangeException(
                nameof(workerId),
                $"WorkerId must be between 0 and {MaxWorkerId}");
        }

        _workerId = workerId;
    }

    public static long NewId()
    {
        lock (Lock)
        {
            var timestamp = GetTimestamp();

            // clock rollback
            if (timestamp < _lastTimestamp)
            {
                throw new InvalidOperationException(
                    "Clock moved backwards.");
            }

            if (timestamp == _lastTimestamp)
            {
                _sequence = (_sequence + 1) & MaxSequence;

                // sequence overflow
                if (_sequence == 0)
                {
                    timestamp = WaitNextMillis(timestamp);
                }
            }
            else
            {
                _sequence = 0;
            }

            _lastTimestamp = timestamp;

            return (timestamp << (WorkerIdBits + SequenceBits))
                   | (_workerId << SequenceBits)
                   | _sequence;
        }
    }

    private static long GetTimestamp()
    {
        return (long)(DateTime.UtcNow - Epoch).TotalMilliseconds;
    }

    private static long WaitNextMillis(long currentTimestamp)
    {
        var timestamp = GetTimestamp();

        while (timestamp <= currentTimestamp)
        {
            timestamp = GetTimestamp();
        }

        return timestamp;
    }
}