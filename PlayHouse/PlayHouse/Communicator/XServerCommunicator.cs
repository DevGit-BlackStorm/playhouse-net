using PlayHouse.Communicator.PlaySocket;

namespace PlayHouse.Communicator
{
    
    public class XServerCommunicator : IServerCommunicator
    {
        private readonly IPlaySocket _playSocket;
        private readonly ILogger _log ;

        private ICommunicateListener? _listener;
        private bool _running = true;

        public XServerCommunicator(IPlaySocket playSocket,ILogger logger)
        {
            _playSocket = playSocket;
            _log = logger;
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
                        _listener!.OnReceive(packet);
                    }
                    catch (Exception e)
                    {

                        _log.Error($"{_playSocket.Id()} Error during communication", nameof(XServerCommunicator), e);
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
