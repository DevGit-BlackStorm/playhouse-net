using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Communicator
{
    public class MessageLoop
    {
        private readonly IServerCommunicator _server;
        private readonly IClientCommunicator _client;
        private readonly ILogger _log;
        private readonly Thread _serverThread;
        private readonly Thread _clientThread;

        public MessageLoop(IServerCommunicator server, IClientCommunicator client,ILogger logger)
        {
            _server = server;
            _client = client;
            _log = logger;

            _serverThread = new Thread(() =>
            {
                _log.Info("start Server Communicator",nameof(MessageLoop));
                _server.Communicate();
            })
            {
                Name = "server:Communicator"
            };

            _clientThread = new Thread(() =>
            {
                _log.Info("start client Communicator", nameof(MessageLoop));
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




}

