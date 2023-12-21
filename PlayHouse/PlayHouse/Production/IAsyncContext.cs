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

public interface IAsyncCore
{
    public void Init(IApiSender? apiSender = null);
    public ushort GetErrorCode();
    public IApiSender GetApiSender();
    public List<(SendTarget target, IPacket packet)> GetSendPackets();

    public void SetErrorCode(ushort errorCode);
    public void SetApiSender(IApiSender apiSender);
    public void Add(SendTarget target,IPacket packet);

    public void Clear();

}
