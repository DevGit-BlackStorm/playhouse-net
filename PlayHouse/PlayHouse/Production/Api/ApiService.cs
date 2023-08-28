using PlayHouse.Communicator.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Production.Api
{
    public delegate Task ApiHandler(Packet packet, IApiSender apiSender);
    public delegate Task ApiBackendHandler(Packet packet, IApiBackendSender apiSender);

    public interface IHandlerRegister
    {
        void Add(int msgId, ApiHandler handler);
    }

    public interface IBackendHandlerRegister
    {
        void Add(int msgId, ApiBackendHandler handler);
    }

    public interface IApiController
    {
        //Task Init(ISystemPanel systemPanel, ISender sender);
        void Handles(IHandlerRegister handlerRegister, IBackendHandlerRegister backendHandlerRegister);
        IApiController Instance();
    }

    //public interface IApiBackendService
    //{
    //    Task Init(ISystemPanel systemPanel, ISender sender);
    //    void Handles(IBackendHandlerRegister register);
    //    IApiBackendService Instance();
    //}
}
