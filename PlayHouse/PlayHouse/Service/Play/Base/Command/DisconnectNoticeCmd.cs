using PlayHouse.Communicator.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service.Play.Base.Command
{
    public class DisconnectNoticeCmd : IBaseStageCmd
    {
        private readonly PlayProcessor _playProcessor;
        public PlayProcessor PlayProcessor => _playProcessor;
        public DisconnectNoticeCmd(PlayProcessor playProcessor)
        {
            _playProcessor = playProcessor;
        }

        public  async Task Execute(BaseStage baseStage, RoutePacket routePacket)
        {
            var accountId = routePacket.AccountId;
            await baseStage.OnDisconnect(accountId);
        }
    }

}
