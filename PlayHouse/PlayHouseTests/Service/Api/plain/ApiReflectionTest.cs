using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Org.Ulalax.Playhouse.Protocol;
using PlayHouse.Communicator.Message;
using PlayHouse.Production.Api.Aspectify;
using PlayHouse.Production.Shared;
using PlayHouse.Service.Api.Reflection;
using PlayHouse.Service.Shared;
using PlayHouse.Service.Shared.Reflection;
using Xunit;

namespace PlayHouseTests.Service.Api.plain
{
    class ReflectionTestResult
    {
        public static Dictionary<string, string> ResultMap = new();
    }

    public class ApiReflectionTest
    {

        [Fact]
        public async Task Test_CALL_Method()
        {

            PacketProducer.Init((int msgId, IPayload payload,ushort msgSeq) => new TestPacket(msgId, payload, msgSeq));

            ApiControllAspectifyManager controllAspectifyManager = new ApiControllAspectifyManager();

            controllAspectifyManager.Add(new TestGlobalAspectifyAttribute());

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<TestApiController>();
            var apiReflections = new ApiReflection(serviceCollection.BuildServiceProvider(), controllAspectifyManager);

            var apiSender = new Mock<IApiSender>().Object;
            bool isBackend = false;

            
            var routePacket = RoutePacket.ApiOf(RoutePacket.Of(new ApiTestMsg1() { TestMsg = "ApiServiceCall_Test1" }), false, isBackend);

            await apiReflections.CallMethodAsync(routePacket.ToContentsPacket(), apiSender);

            ReflectionTestResult.ResultMap["TestApiController_Test1"].Should().Be("ApiServiceCall_Test1");

            ReflectionTestResult.ResultMap[$"TestApiGlobalActionAttributeBefore_{ApiTestMsg1.Descriptor.Index}"].Should().Be("BeforeExecution");
            ReflectionTestResult.ResultMap[$"TestApiGlobalActionAttributeAfter_{ApiTestMsg1.Descriptor.Index}"].Should().Be("AfterExecution");

            ReflectionTestResult.ResultMap[$"TestApiActionAttributeBefore_{ApiTestMsg1.Descriptor.Index}"].Should().Be("BeforeExecution");
            ReflectionTestResult.ResultMap[$"TestApiActionAttributeAfter_{ApiTestMsg1.Descriptor.Index}"].Should().Be("AfterExecution");
            ReflectionTestResult.ResultMap[$"TestApiMethodActionAttributeBefore_{ApiTestMsg1.Descriptor.Index}"].Should().Be("BeforeExecution");
            ReflectionTestResult.ResultMap[$"TestApiMethodActionAttributeAfter_{ApiTestMsg1.Descriptor.Index}"].Should().Be("AfterExecution");
            

            routePacket = RoutePacket.ApiOf(RoutePacket.Of(new ApiTestMsg2() { TestMsg = "ApiServiceCall_Test2" }), false, isBackend);
            await apiReflections.CallMethodAsync(routePacket.ToContentsPacket(), apiSender);

            ReflectionTestResult.ResultMap[$"TestApiGlobalActionAttributeBefore_{ApiTestMsg2.Descriptor.Index}"].Should().Be("BeforeExecution");
            ReflectionTestResult.ResultMap[$"TestApiGlobalActionAttributeAfter_{ApiTestMsg2.Descriptor.Index}"].Should().Be("AfterExecution");

            ReflectionTestResult.ResultMap["TestApiController_Test2"].Should().Be("ApiServiceCall_Test2");
            ReflectionTestResult.ResultMap[$"TestApiActionAttributeBefore_{ApiTestMsg2.Descriptor.Index}"].Should().Be("BeforeExecution");
            ReflectionTestResult.ResultMap[$"TestApiActionAttributeAfter_{ApiTestMsg2.Descriptor.Index}"].Should().Be("AfterExecution");
            ReflectionTestResult.ResultMap.ContainsKey($"TestApiMethodActionAttributeBefore_{ApiTestMsg2.Descriptor.Index}").Should().BeFalse();
            ReflectionTestResult.ResultMap.ContainsKey($"TestApiMethodActionAttributeAfter_{ApiTestMsg2.Descriptor.Index}").Should().BeFalse();

            //await callbackReflection.InvokeCallbackMethods("OnDisconnectAsync", apiSender);
            //ReflectionTestResult.ResultMap["OnDisconnectAsync"].Should().Be("OnDisconnectAsync");


        }

        [Fact]
        public async Task Test_CALL_Backend_Method()
        {
            PacketProducer.Init((int msgId, IPayload payload, ushort msgSeq) => new TestPacket(msgId, payload, msgSeq));

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<TestBackendApiController>();
            var apiReflections = new ApiReflection(serviceCollection.BuildServiceProvider(), new());

            var apiSender = new Mock<IApiBackendSender>().Object;
            bool isBackend = false;

            var routePacket = RoutePacket.ApiOf(RoutePacket.Of(new ApiTestMsg1() { TestMsg = "ApiBackendServiceCall_Test1" }), false, isBackend);

            await apiReflections.CallBackendMethodAsync(routePacket.ToContentsPacket(), apiSender);

            ReflectionTestResult.ResultMap["TestBackendApiController_Test3"].Should().Be("ApiBackendServiceCall_Test1");


            ReflectionTestResult.ResultMap[$"TestBackendApiActionAttributeBefore_{ApiTestMsg1.Descriptor.Index}"].Should().Be("BeforeExecution");
            ReflectionTestResult.ResultMap[$"TestBackendApiActionAttributeAfter_{ApiTestMsg1.Descriptor.Index}"].Should().Be("AfterExecution");
            ReflectionTestResult.ResultMap[$"TestBackendApiMethodActionAttributeBefore_{ApiTestMsg1.Descriptor.Index}"].Should().Be("BeforeExecution");
            ReflectionTestResult.ResultMap[$"TestBackendApiMethodActionAttributeAfter_{ApiTestMsg1.Descriptor.Index}"].Should().Be("AfterExecution");


            routePacket = RoutePacket.ApiOf(RoutePacket.Of(new ApiTestMsg2() { TestMsg = "ApiBackendServiceCall_Test2" }), false, isBackend);

            await apiReflections.CallBackendMethodAsync(routePacket.ToContentsPacket(), apiSender);

            ReflectionTestResult.ResultMap["TestBackendApiController_Test4"].Should().Be("ApiBackendServiceCall_Test2");


            ReflectionTestResult.ResultMap[$"TestBackendApiActionAttributeBefore_{ApiTestMsg2.Descriptor.Index}"].Should().Be("BeforeExecution");
            ReflectionTestResult.ResultMap[$"TestBackendApiActionAttributeAfter_{ApiTestMsg2.Descriptor.Index}"].Should().Be("AfterExecution");
            ReflectionTestResult.ResultMap.ContainsKey($"TestBackendApiMethodActionAttributeBefore_{ApiTestMsg2.Descriptor.Index}").Should().BeFalse();
            ReflectionTestResult.ResultMap.ContainsKey($"TestBackendApiMethodActionAttributeAfter_{ApiTestMsg2.Descriptor.Index}").Should().BeFalse();
        }

        [Fact]
        public async Task Disconnect_callback_should_be_called()
        {
            PacketProducer.Init((int msgId, IPayload payload, ushort msgSeq) => new TestPacket(msgId, payload, msgSeq));
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<TestApiController>();
            var callbackReflection = new ApiReflectionCallback(serviceCollection.BuildServiceProvider());

            var apiSenderMock = new Mock<IApiSender>();
            await callbackReflection.OnDisconnectAsync(apiSenderMock.Object);

        }

        [Fact]
        public async Task UpdateServerInfo_callback_should_be_called()
        {
            PacketProducer.Init((int msgId, IPayload payload, ushort msgSeq) => new TestPacket(msgId, payload, msgSeq));
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<TestSystemController>();

            var callbackReflection = new ApiReflectionCallback(serviceCollection.BuildServiceProvider());

            var serverInfo = new Mock<IServerInfo>();
            List<IServerInfo> serverInfos = await callbackReflection.UpdateServerInfoAsync(serverInfo.Object);
            serverInfos.Count.Should().Be(1);
            ReflectionTestResult.ResultMap["TestSystemController_UpdateServerInfoAsync"].Should().Be("UpdateServerInfoAsync");
        }


        [Fact]
        public async Task SystemController_method_should_be_called()
        {
            PacketProducer.Init((int msgId, IPayload payload, ushort msgSeq) => new TestPacket(msgId, payload, msgSeq));
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddScoped<TestSystemController>();

            var systemReflection = new SystemReflection(serviceCollection.BuildServiceProvider());

            var systemPanel = new Mock<ISystemPanel>().Object;
            var sender =  new Mock<ISender>().Object;
            await systemReflection.CallMethodAsync(new TestPacket(new SystemHandlerTestMsg() { TestMsg = "SystemMethod" }),systemPanel,sender);

            ReflectionTestResult.ResultMap["TestSystemController_Test"].Should().Be("SystemMethod");

            //var serverInfo = new Mock<IServerInfo>();
            //List<IServerInfo> serverInfos = await callbackReflection.UpdateServerInfoAsync(serverInfo.Object);
            //serverInfos.Count.Should().Be(1);
            //ReflectionTestResult.ResultMap["TestSystemController_UpdateServerInfoAsync"].Should().Be("UpdateServerInfoAsync");
        }

    }
}
