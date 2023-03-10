using PlayHouse.Communicator.Message;
using PlayHouse.Communicator.PlaySocket;
using System.Net;


namespace PlayHouse.Communicator
{

    public class XClientCommunicator : IClientCommunicator
    {
        private readonly IPlaySocket _playSocket;
        private readonly ILogger _log;

        private readonly HashSet<string> _connected = new HashSet<string>();
        private readonly HashSet<string> _disconnected = new HashSet<string>();
        private readonly JobBucket _jobBucket = new JobBucket();
        private bool _running = true;

        public XClientCommunicator(IPlaySocket playSocket,ILogger logger)
        {
            _playSocket = playSocket;
            _log = logger;
        }

        public void Connect(string endpoint)
        {
            if (_connected.Contains(endpoint))
            {
                return;
            }

            _jobBucket.Add(() =>
            {
                _playSocket.Connect(endpoint);
                _connected.Add(endpoint);
                _disconnected.Remove(endpoint);
            });
        }

        public void Disconnect(string endpoint)
        {
            if (_disconnected.Contains(endpoint))
            {
                return;
            }

            _jobBucket.Add(() =>
            {
                _playSocket.Disconnect(endpoint);
                _disconnected.Add(endpoint);
                _connected.Remove(endpoint);
            });
        }

        public void Stop()
        {
            _running = false;
        }

        public void Send(string endpoint, RoutePacket routePacket)
        {
            _jobBucket.Add(() =>
            {
                try
                {
                    using (routePacket)
                    {
                        _playSocket.Send(endpoint, routePacket);
                    }
                }
                catch (Exception e)
                {
                    _log.Error($"{_playSocket.Id()} socket send error : {endpoint},{routePacket.MsgName}", nameof(XClientCommunicator),e);
                }
            });
        }

        public void Communicate()
        {
            while (_running)
            {
                var action = _jobBucket.Get();
                while (action != null)
                {
                    try
                    {
                        action.Invoke();
                    }
                    catch (Exception e)
                    {
                        _log.Error($"{_playSocket.Id()} Error during communication", nameof(XClientCommunicator), e);
                    }
                    action = _jobBucket.Get();
                }
                Thread.Sleep(ConstOption.ThreadSleep);
            }
        }
    }
}
