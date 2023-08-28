using Microsoft.Win32;
using Org.Ulalax.Playhouse.Protocol;
using PlayHouse.Production;
using PlayHouse.Production.Api;

namespace PlayHouseTests.Service.Api.plain
{

    public class TestApiService : IApiController
    {
        private readonly ISystemPanel _systemPanel;
        private readonly ISender _sender;

        public TestApiService(ISystemPanel systemPanel,ISender sender)
        {
            _systemPanel = systemPanel;
            _sender = sender;
        }

        public void Handles(IHandlerRegister handlerRegister,IBackendHandlerRegister backendHandlerRegister)
        {
            handlerRegister.Add(ApiTestMsg1.Descriptor.Index, Test1);
            handlerRegister.Add(ApiTestMsg2.Descriptor.Index, Test2);

            backendHandlerRegister.Add(ApiTestMsg1.Descriptor.Index, Test3);
            backendHandlerRegister.Add(ApiTestMsg2.Descriptor.Index, Test4);
        }


        public async Task Test1(Packet packet, IApiSender apiSender)
        {
            var message = ApiTestMsg1.Parser.ParseFrom(packet.Data);
            ReflectionTestResult.ResultMap[$"{this.GetType().Name}_Test1"] = message.TestMsg;
            await Task.CompletedTask;
        }

        public async Task Test2(Packet packet, IApiSender apiSender)
        {
            var message = ApiTestMsg2.Parser.ParseFrom(packet.Data);
            ReflectionTestResult.ResultMap[$"{this.GetType().Name}_Test2"] = message.TestMsg;
            await Task.CompletedTask;
        }

        public async Task Test3(Packet packet, IApiBackendSender apiSender)
        {
            var message = ApiTestMsg1.Parser.ParseFrom(packet.Data);
            ReflectionTestResult.ResultMap[$"{this.GetType().Name}_Test3"] = message.TestMsg;
            await Task.CompletedTask;
        }

        public async Task Test4(Packet packet, IApiBackendSender apiSender)
        {
            var message = ApiTestMsg2.Parser.ParseFrom(packet.Data);
            ReflectionTestResult.ResultMap[$"{this.GetType().Name}_Test4"] = message.TestMsg;
            await Task.CompletedTask;
        }
    }
}
