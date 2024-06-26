﻿namespace PlayHouse.Communicator;

public static class ConstOption
{
    public const int ThreadSleep = 10;

    public const int SessionBufferSize = 4 * 1024;

    public const int MaxPacketSize = 2097152;

    //public const int HeaderSize = 256;
    public const int LengthFieldSize = 3;
    public const int MinClientHeaderSize = 18;
    public const int MinServerHeaderSize = 20;
    public static int AddressResolverInitialDelayMs { get; internal set; } = 1000;
    public static int AddressResolverPeriodMs { get; internal set; } = 1000;
    public static ushort DefaultServiceId { get; internal set; } = 0;
    public static int StopDelayMs { get; internal set; } = 3000;
}