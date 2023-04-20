using PlayHouse.Communicator.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service.Play
{
    public interface IStage<A> where A : IActor
    {
        public IStageSender StageSender { get; }

        public Task<ReplyPacket> OnCreate(Packet packet);
        public Task<ReplyPacket> OnJoinStage(A actor, Packet packet);
        public Task OnDispatch(A actor, Packet packet);
        public Task OnDisconnect(A actor);
        public Task OnPostCreate();
        public Task OnPostJoinStage(A actor);
    }
}
