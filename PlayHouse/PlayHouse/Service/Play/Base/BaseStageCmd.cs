using PlayHouse.Communicator.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service.Play.Base
{
    public interface IBaseStageCmd
    {
        public PlayProcessor PlayProcessor { get; }
        public Task Execute(BaseStage baseStage, RoutePacket routePacket);
    }
}
