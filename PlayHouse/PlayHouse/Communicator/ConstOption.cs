namespace PlayHouse.Communicator
{
    public static class ConstOption
    {
        public const int ThreadSleep = 10;
        public static int AddressResolverInitialDelay { get; internal set; } = 3000;
        public static int AddressResolverPeriod { get; internal set; } = 3000;
        public static ushort DefaultServiceId { get; internal set; } = 0;

        public const int SessionBufferSize = 4 * 1024;

        public const int MaxPacketSize = 65535;
        public const int HeaderSize = 256;
        public const int LengthFieldSize = 3;
    }
}
