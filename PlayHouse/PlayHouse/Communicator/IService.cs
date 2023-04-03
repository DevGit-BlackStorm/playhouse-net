using PlayHouse.Communicator.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator
{
    public enum ServiceType
    {
        SESSION,
        API,
        Play
    }

    public interface IService
    {
        void OnStart();
        void OnReceive(RoutePacket routePacket);
        void OnStop();
        int GetWeightPoint();
        ServerState GetServerState();
        ServiceType GetServiceType();
        short GetServiceId();
        void Pause();
        void Resume();
    }

}
