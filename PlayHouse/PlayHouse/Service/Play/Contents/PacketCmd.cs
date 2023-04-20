using PlayHouse.Communicator.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service.Play.Contents
{
    public interface IPacketCmd<S, A> where A : IActor
    {
        public Task Execute(S stage, A actor, Packet packet);
    }
}
