namespace PlayHouse.Service.Shared;

public class UniqueIdGenerator
{
    private const long NodeIdBits = 12L;
    private const long SequenceBits = 10L;
    private const long NodeIdShift = 10L;
    private const long TimestampLeftShift = NodeIdBits + SequenceBits;

    private static readonly long
        Epoch = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();

    private readonly long _nodeId;
    private long _lastTimestamp = -1L;

    private long _sequence;

    public UniqueIdGenerator(int nodeId)
    {
        if (nodeId < 0 || nodeId > 4095)
        {
            throw new ArgumentOutOfRangeException(nameof(nodeId), "Node ID must be between 0 and 4095.");
        }

        _nodeId = nodeId;
    }

    public long NextId()
    {
        lock (this)
        {
            var timestamp = GetCurrentTimestamp();

            if (timestamp < _lastTimestamp)
            {
                throw new InvalidOperationException("Invalid system clock.");
            }

            if (timestamp == _lastTimestamp)
            {
                _sequence = (_sequence + 1) & ((1L << (int)SequenceBits) - 1);

                if (_sequence == 0)
                {
                    timestamp = WaitForNextTimestamp(timestamp);
                }
            }
            else
            {
                _sequence = 0;
            }

            _lastTimestamp = timestamp;

            return ((timestamp - Epoch) << (int)TimestampLeftShift) |
                   (_nodeId << (int)NodeIdShift) |
                   _sequence;
        }
    }

    private long GetCurrentTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    private long WaitForNextTimestamp(long currentTimestamp)
    {
        var timestamp = GetCurrentTimestamp();
        while (timestamp <= currentTimestamp)
        {
            timestamp = GetCurrentTimestamp();
        }

        return timestamp;
    }
}