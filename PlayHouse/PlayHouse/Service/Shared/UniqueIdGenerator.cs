namespace PlayHouse.Service.Shared;

using System;
public class UniqueIdGenerator
{
    private readonly long _nodeId;
    private static readonly long _nodeIdBits = 12L;
    private static readonly long _sequenceBits = 10L;
    private static readonly long _nodeIdShift = 10L;
    private static readonly long _timestampLeftShift = _nodeIdBits + _sequenceBits;

    private long _sequence = 0L;
    private long _lastTimestamp = -1L;

    private static readonly long Epoch = new DateTimeOffset(2020, 1, 1, 0, 0, 0, TimeSpan.Zero).ToUnixTimeMilliseconds();

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
            long timestamp = GetCurrentTimestamp();

            if (timestamp < _lastTimestamp)
            {
                throw new InvalidOperationException("Invalid system clock.");
            }

            if (timestamp == _lastTimestamp)
            {
                _sequence = _sequence + 1 & (1L << (int)_sequenceBits) - 1;

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

            return timestamp - Epoch << (int)_timestampLeftShift |
                   _nodeId << (int)_nodeIdShift |
                   _sequence;
        }
    }

    private long GetCurrentTimestamp()
    {
        return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    private long WaitForNextTimestamp(long currentTimestamp)
    {
        long timestamp = GetCurrentTimestamp();
        while (timestamp <= currentTimestamp)
        {
            timestamp = GetCurrentTimestamp();
        }
        return timestamp;
    }
}

