using Playhouse.Protocol;
using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service.Api
{
    public class AllApiSender : XApiCommonSender, IApiSender, IApiBackendSender
    {
        private short serviceId;
        private IClientCommunicator clientCommunicator;
        private RequestCache reqCache;

        public AllApiSender(short serviceId, IClientCommunicator clientCommunicator, RequestCache reqCache)
            : base(serviceId, clientCommunicator, reqCache)
        {
            this.serviceId = serviceId;
            this.clientCommunicator = clientCommunicator;
            this.reqCache = reqCache;
        }

        public string GetFromEndpoint()
        {
            return _currentHeader?.From ?? "";
        }

        public string SessionEndpoint()
        {
            return _currentHeader?.From ?? "";
        }

        public int Sid => _currentHeader?.Sid ?? 0;

        public void Authenticate(long accountId)
        {
            var message = new AuthenticateMsg()
            {
                ServiceId = (int)serviceId,
                AccountId = accountId
            };

            if (_currentHeader != null)
            {
                SendToBaseSession(_currentHeader.From, _currentHeader.Sid, new Packet(message));
            }
            else
            {
                throw new ApiException.NotExistApiHeaderInfoException();
            }
        }

        public AllApiSender Clone()
        {
            return new AllApiSender(this.serviceId, this.clientCommunicator, this.reqCache);
        }
    }
}
