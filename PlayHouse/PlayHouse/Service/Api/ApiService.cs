using PlayHouse.Communicator.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service.Api
{
    public delegate void ApiHandler(Packet packet, IApiSender apiSender);
    public delegate void ApiBackendHandler(Packet packet, IApiBackendSender apiSender);

    public interface IHandlerRegister
    {
        void Add(int msgId, ApiHandler handler);
    }

    public interface IBackendHandlerRegister
    {
        void Add(int msgId, ApiBackendHandler handler);
    }

    public interface IApiService
    {
        void Init(ISystemPanel systemPanel, ISender sender);
        void Handles(IHandlerRegister register);
        IApiService Instance();
    }

    public interface IApiBackendService
    {
        void Init(ISystemPanel systemPanel, ISender sender);
        void Handles(IBackendHandlerRegister register);
        IApiBackendService Instance();
    }
}
