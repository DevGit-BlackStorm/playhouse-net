using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator.Message;

public interface IBasePacket : IDisposable
{
    public IPayload MovePayload();
    public ReadOnlyMemory<byte> Data { get; }
    public ReadOnlySpan<byte> Span => Data.Span;
}
