using PlayHouse.Production;
using PlayHouse.Utils;

namespace PlayHouse.Communicator;
internal class MessageLoop
{
    private readonly IServerCommunicator _server;
    private readonly IClientCommunicator _client;
    private readonly Thread _serverThread;
    private readonly Thread _clientThread;
    private readonly LOG<MessageLoop> _log = new ();

    public MessageLoop(IServerCommunicator server, IClientCommunicator client)
    {
        _server = server;
        _client = client;

        _serverThread = new Thread(() =>
        {
            _log.Info(()=>"start Server Communicator");
            _server.Communicate();
        })
        {
            Name = "server:Communicator"
        };

        _clientThread = new Thread(() =>
        {
            _log.Info(()=>"start client Communicator");
            _client.Communicate();
        })
        {
            Name = "client:Communicator"
        };
    }

    public void Start()
    {
        _serverThread.Start();
        _clientThread.Start();
    }

    public void Stop()
    {
        _server.Stop();
        _client.Stop();
    }

    public void AwaitTermination()
    {
        _clientThread.Join();
        _serverThread.Join();
    }
}


