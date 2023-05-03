using PlayHouse.Production;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Production.Play
{
    public interface IStage
    {
        public IStageSender StageSender { get; }

        public Task<ReplyPacket> OnCreate(Packet packet);
        public Task<ReplyPacket> OnJoinStage(object actor, Packet packet);
        public Task OnDispatch(object actor, Packet packet);
        public Task OnDisconnect(object actor);
        public Task OnPostCreate();
        public Task OnPostJoinStage(object actor);
    }
}
