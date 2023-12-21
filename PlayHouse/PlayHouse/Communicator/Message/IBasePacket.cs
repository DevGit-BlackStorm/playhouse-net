using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator.Message
{
    public interface IBasePacket : IDisposable
    {
        IPayload MovePayload();
        ReadOnlySpan<byte> Data { get; }
    }
}
