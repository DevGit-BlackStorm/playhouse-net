using PlayHouse.Communicator.Message;
using PlayHouse.Communicator.PlaySocket;
using PlayHouse.Production;
using System.Net;


namespace PlayHouse.Communicator
{

    public class XClientCommunicator : IClientCommunicator
    {
        private readonly IPlaySocket _playSocket;

        private readonly HashSet<string> _connected = new HashSet<string>();
        private readonly HashSet<string> _disconnected = new HashSet<string>();
        private readonly JobBucket _jobBucket = new JobBucket();
        private bool _running = true;

        public XClientCommunicator(IPlaySocket playSocket)
        {
            _playSocket = playSocket;
        }

        public void Connect(string endpoint)
        {
            if (_connected.Contains(endpoint))
            {
                return;
            }

            _jobBucket.Add(() =>
            {
                try
                {
                    _playSocket.Connect(endpoint);
                    _connected.Add(endpoint);
                    _disconnected.Remove(endpoint);
                    LOG.Info($"connected with {endpoint}", this.GetType());
                }
                catch(Exception ex) 
                {
                    LOG.Error($"connect error - endpoint:{endpoint}, error:{ex.Message}", this.GetType());
                }
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
                try
                {
                    _playSocket.Disconnect(endpoint);
                    LOG.Info($"disconnected with {endpoint}", this.GetType());
                }
                catch(Exception ex)
                {
                    LOG.Error($"disconnect error - endpoint:{endpoint}, error:{ex.Message}", this.GetType());
                    
                }finally {
                    _connected.Remove(endpoint);
                    _disconnected.Add(endpoint); 
                }
                
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
                    LOG.Error($"{_playSocket.Id()} socket send error : {endpoint},{routePacket.MsgId}", this.GetType(),e);
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
                        LOG.Error($"{_playSocket.Id()} Error during communication", this.GetType(), e);
                    }
                    action = _jobBucket.Get();
                }
                Thread.Sleep(ConstOption.ThreadSleep);
            }
        }
    }
}
