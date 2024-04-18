using PlayHouse.Communicator.Message;
using Playhouse.Protocol;
using PlayHouse.Communicator.PlaySocket;
using PlayHouse.Utils;

namespace PlayHouse.Communicator
{
    internal class XServerCommunicator : IServerCommunicator
    {
        private readonly IPlaySocket _playSocket;

        private ICommunicateListener? _listener;
        private bool _running = true;
        private readonly LOG<XServerCommunicator> _log = new ();

        public XServerCommunicator(IPlaySocket playSocket)
        {
            _playSocket = playSocket;
        }

        public void Bind(ICommunicateListener listener)
        {
            _listener = listener;
            _playSocket.Bind();
        }


        public void Communicate()
        {
            while (_running)
            {
                var packet = _playSocket.Receive();
                while (packet != null)
                {
                    try
                    {
                        if (packet.MsgId != UpdateServerInfoReq.Descriptor.Index && packet.MsgId != UpdateServerInfoRes.Descriptor.Index)
                        {
                            _log.Trace(() => $"recvFrom:{packet.RouteHeader.From} - [packetInfo:${packet.RouteHeader}]");
                        }
                         
                        _listener!.OnReceive(packet);
                    }
                    catch (Exception e)
                    {

                        _log.Error(()=>$"{_playSocket.Id()} Error during communication - {e.Message}");
                    }

                    packet = _playSocket.Receive();
                }
                Thread.Sleep(ConstOption.ThreadSleep);
            }
        }

        public void Stop()
        {
            _running = false;
        }
    }

}
