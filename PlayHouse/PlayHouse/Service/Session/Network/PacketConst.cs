namespace PlayHouse.Service.Session.Network;

internal static class PacketConst
{
    public static readonly int MsgIdLimit = 256;
    public static readonly int MaxPacketSize = 1024 * 1024 * 2;
    public static readonly int MinPacketSize = 17;
    public static int MinCompressionSize = 1500;
    public static readonly string HeartBeat = "@Heart@Beat@";
    public static readonly string Debug = "@Debug@";
    public static readonly string Timeout = "@Timeout@";
}