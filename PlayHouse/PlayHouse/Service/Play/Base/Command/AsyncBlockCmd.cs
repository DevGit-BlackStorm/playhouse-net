using PlayHouse.Communicator.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service.Play.Base.Command
{
    public class AsyncBlockCmd : IBaseStageCmd
    {
        private readonly PlayProcessor _playProcessor;

        public PlayProcessor PlayProcessor => _playProcessor;

        public AsyncBlockCmd(PlayProcessor playProcessor)
        {
            _playProcessor = playProcessor;
        }

        public  async Task Execute(BaseStage baseStage, RoutePacket routePacket)
        {
            AsyncBlockPacket asyncBlock = (AsyncBlockPacket)routePacket;
            await asyncBlock.AsyncPostCallback!.Invoke(asyncBlock.Result);
        }
    }

}
