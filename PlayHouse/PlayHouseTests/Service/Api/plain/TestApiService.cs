using Org.Ulalax.Playhouse.Protocol;
using PlayHouse.Communicator.Message;
using PlayHouse.Service;
using PlayHouse.Service.Api;

namespace PlayHouseTests.Service.Api.plain
{
    internal class TestApiService : IApiService
    {
        private ISystemPanel _systemPanel;
        private ISender _sender;

        public TestApiService()
        {

        }

        public void Init(ISystemPanel systemPanel, ISender sender)
        {
            _systemPanel = systemPanel;
            _sender = sender;
            ReflectionTestResult.ResultMap[$"{this.GetType().Name}_Init"] = "OK";

        }
        public void Handles(IHandlerRegister register)
        {
            register.Add(ApiTestMsg1.Descriptor.Index, Test1);
            register.Add(ApiTestMsg2.Descriptor.Index, Test2);
        }

        public IApiService Instance()
        {
            return new TestApiService();
        }

        public void Test1(Packet packet, IApiSender apiSender)
        {
            var message = ApiTestMsg1.Parser.ParseFrom(packet.Data);
            ReflectionTestResult.ResultMap[$"{this.GetType().Name}_Test1"] = message.TestMsg;
        }

        public void Test2(Packet packet, IApiSender apiSender)
        {
            var message = ApiTestMsg2.Parser.ParseFrom(packet.Data);
            ReflectionTestResult.ResultMap[$"{this.GetType().Name}_Test2"] = message.TestMsg;
        }
    }
}
