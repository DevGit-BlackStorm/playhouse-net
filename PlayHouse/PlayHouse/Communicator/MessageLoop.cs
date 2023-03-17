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
        private readonly Thread _serverThread;
        private readonly Thread _clientThread;

        public MessageLoop(IServerCommunicator server, IClientCommunicator client)
        {
            _server = server;
            _client = client;

            _serverThread = new Thread(() =>
            {
                LOG.Info("start Server Communicator",this.GetType());
                _server.Communicate();
            })
            {
                Name = "server:Communicator"
            };

            _clientThread = new Thread(() =>
            {
                LOG.Info("start client Communicator", this.GetType());
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

