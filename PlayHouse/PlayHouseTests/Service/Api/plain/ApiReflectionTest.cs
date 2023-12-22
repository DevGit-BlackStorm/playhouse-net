using FluentAssertions;
using Moq;
using Org.Ulalax.Playhouse.Protocol;
using PlayHouse.Communicator.Message;
using PlayHouse.Production;
using PlayHouse.Service;
using PlayHouse.Service.Api;
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

            PacketProducer.Init((int msgId, IPayload payload) => new TestPacket(msgId, payload));

            var apiReflections = new ApiReflection();
            var apiSender = new Mock<IApiSender>().Object; 
            bool isBackend = false;

            
            var routePacket = RoutePacket.ApiOf(RoutePacket.Of(new ApiTestMsg1() { TestMsg = "ApiServiceCall_Test1" }), false, isBackend);

            await apiReflections.CallMethod(routePacket.RouteHeader, routePacket, apiSender);

            ReflectionTestResult.ResultMap["TestApiController_Test1"].Should().Be("ApiServiceCall_Test1");
            ReflectionTestResult.ResultMap[$"TestApiActionAttributeBefore_{ApiTestMsg1.Descriptor.Index}"].Should().Be("BeforeExecution");
            ReflectionTestResult.ResultMap[$"TestApiActionAttributeAfter_{ApiTestMsg1.Descriptor.Index}"].Should().Be("AfterExecution");
            ReflectionTestResult.ResultMap[$"TestApiMethodActionAttributeBefore_{ApiTestMsg1.Descriptor.Index}"].Should().Be("BeforeExecution");
            ReflectionTestResult.ResultMap[$"TestApiMethodActionAttributeAfter_{ApiTestMsg1.Descriptor.Index}"].Should().Be("AfterExecution");
            

            routePacket = RoutePacket.ApiOf(RoutePacket.Of(new ApiTestMsg2() { TestMsg = "ApiServiceCall_Test2" }), false, isBackend);
            await apiReflections.CallMethod(routePacket.RouteHeader, routePacket, apiSender);

            ReflectionTestResult.ResultMap["TestApiController_Test2"].Should().Be("ApiServiceCall_Test2");
            ReflectionTestResult.ResultMap[$"TestApiActionAttributeBefore_{ApiTestMsg2.Descriptor.Index}"].Should().Be("BeforeExecution");
            ReflectionTestResult.ResultMap[$"TestApiActionAttributeAfter_{ApiTestMsg2.Descriptor.Index}"].Should().Be("AfterExecution");
            ReflectionTestResult.ResultMap.ContainsKey($"TestApiMethodActionAttributeBefore_{ApiTestMsg2.Descriptor.Index}").Should().BeFalse();
            ReflectionTestResult.ResultMap.ContainsKey($"TestApiMethodActionAttributeAfter_{ApiTestMsg2.Descriptor.Index}").Should().BeFalse();

        }

        [Fact]
        public async Task Test_CALL_Backend_Method()
        {
            PacketProducer.Init((int msgId,IPayload payload) =>  new TestPacket(msgId, payload));
            var apiReflections = new ApiReflection();
            var apiSender = new Mock<IApiBackendSender>().Object;
            bool isBackend = false;

            var routePacket = RoutePacket.ApiOf(RoutePacket.Of(new ApiTestMsg1() { TestMsg = "ApiBackendServiceCall_Test1" }), false, isBackend);

            await apiReflections.BackendCallMethod(routePacket.RouteHeader, routePacket, apiSender);

            ReflectionTestResult.ResultMap["TestApiController_Test3"].Should().Be("ApiBackendServiceCall_Test1");
            ReflectionTestResult.ResultMap[$"TestBackendApiActionAttributeBefore_{ApiTestMsg1.Descriptor.Index}"].Should().Be("BeforeExecution");
            ReflectionTestResult.ResultMap[$"TestBackendApiActionAttributeAfter_{ApiTestMsg1.Descriptor.Index}"].Should().Be("AfterExecution");
            ReflectionTestResult.ResultMap[$"TestBackendApiMethodActionAttributeBefore_{ApiTestMsg1.Descriptor.Index}"].Should().Be("BeforeExecution");
            ReflectionTestResult.ResultMap[$"TestBackendApiMethodActionAttributeAfter_{ApiTestMsg1.Descriptor.Index}"].Should().Be("AfterExecution");



            routePacket = RoutePacket.ApiOf(RoutePacket.Of(new ApiTestMsg2() { TestMsg = "ApiBackendServiceCall_Test2" }), false, isBackend);

            await apiReflections.BackendCallMethod(routePacket.RouteHeader, routePacket, apiSender);

            ReflectionTestResult.ResultMap["TestApiController_Test4"].Should().Be("ApiBackendServiceCall_Test2");
            ReflectionTestResult.ResultMap[$"TestBackendApiActionAttributeBefore_{ApiTestMsg2.Descriptor.Index}"].Should().Be("BeforeExecution");
            ReflectionTestResult.ResultMap[$"TestBackendApiActionAttributeAfter_{ApiTestMsg2.Descriptor.Index}"].Should().Be("AfterExecution");
            ReflectionTestResult.ResultMap.ContainsKey($"TestBackendApiMethodActionAttributeBefore_{ApiTestMsg2.Descriptor.Index}").Should().BeFalse();
            ReflectionTestResult.ResultMap.ContainsKey($"TestBackendApiMethodActionAttributeAfter_{ApiTestMsg2.Descriptor.Index}").Should().BeFalse();
            

        }

    }
}
