using PlayHouse.Communicator;
using PlayHouse.Production.Session;
using PlayHouse.Service.Session.network.tcp;
using PlayHouse.Service.Session.network.websocket;

namespace PlayHouse.Service.Session.network
{
    public class SessionNetwork 
    {
        private ISessionNetwork _sessionNetwork;
        private Thread? _sessionThread;
        private bool _isRunning = true;

        public SessionNetwork(SessionOption sessionOption, ISessionListener sessionListener)
        {
            if (sessionOption.UseWebSocket)
            {
                _sessionNetwork = new WsSessionNetwork(sessionOption, sessionListener);
            }
            else
            {
                _sessionNetwork = new TcpSessionNetwork(sessionOption, sessionListener);
            }
        }

        public void Start()
        {

            _sessionThread = new Thread(() =>
            {
                _sessionNetwork.Start();

                while (_isRunning)
                {
                    Thread.Sleep(100);
                }
            });
            _sessionThread.Start();


        }

        public void Stop()
        {
            _sessionNetwork.Stop();
            _isRunning = false;
        }

        public void Await()
        {
            _sessionThread!.Join();
        }


    }
}