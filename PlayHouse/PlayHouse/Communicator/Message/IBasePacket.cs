namespace PlayHouse.Communicator.Message;

public interface IBasePacket : IDisposable
{
    public ReadOnlyMemory<byte> Data { get; }
    public ReadOnlySpan<byte> Span => Data.Span;
    public IPayload MovePayload();
}