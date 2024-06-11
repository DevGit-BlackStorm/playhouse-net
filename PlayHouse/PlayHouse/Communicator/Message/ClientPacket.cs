namespace PlayHouse.Communicator.Message;

internal class ClientPacket(Header header, IPayload payload) : IBasePacket
{
    public IPayload Payload = payload;

    public Header Header { get; set; } = header;

    public ushort MsgSeq => Header.MsgSeq;
    public string MsgId => Header.MsgId;
    public ushort ServiceId => Header.ServiceId;
    public ReadOnlyMemory<byte> Data => Payload.Data;
    public ReadOnlySpan<byte> Span => Payload.DataSpan;

    public void Dispose()
    {
        Payload.Dispose();
    }

    public IPayload MovePayload()
    {
        var temp = Payload;
        Payload = new EmptyPayload();
        return temp;
    }

    internal RoutePacket ToRoutePacket()
    {
        return RoutePacket.Of(Header.MsgId, MovePayload());
    }
}