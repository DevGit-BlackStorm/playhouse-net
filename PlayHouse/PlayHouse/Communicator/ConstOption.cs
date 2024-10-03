namespace PlayHouse.Communicator;

public static class ConstOption
{
    public const int ThreadSleep = 1;
    public const int MaxPacketSize = 2097152;
    public static int AddressResolverInitialDelayMs { get; internal set; } = 1000;
    public static int AddressResolverPeriodMs { get; internal set; } = 1000;
    public static int StopDelayMs { get; internal set; } = 3000;
}