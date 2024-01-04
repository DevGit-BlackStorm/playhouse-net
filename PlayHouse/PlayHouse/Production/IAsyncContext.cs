using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Production;

public enum SendTarget
{
    ErrorReply = 0,
    Reply = 1,
    Client = 2,
    Api = 3,
    Play = 4,
    System = 5,

};

public class SendPacketInfo
{
    public SendTarget Target { get; set; }
    public ushort ErrorCode { get; set; }
    public IPacket? Packet { get; set; } 
    public ushort MsgSeq { get; set; }
}


public interface IAsyncCore
{
    public void Init();
    public List<SendPacketInfo> GetSendPackets();
    public void Clear();

    public void Add(SendTarget target,ushort msgSeq, IPacket? packet);
    public void Add(SendTarget target, ushort msgSeq, ushort errorCode);

}
