using FluentAssertions;
using Moq;
using Org.Ulalax.Playhouse.Protocol;
using Playhouse.Protocol;
using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production;
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
            var apiReflections = new ApiReflection();
            var apiSender = new Mock<IApiSender>().Object; 
            bool isBackend = false;

            var routePacket = RoutePacket.ApiOf(new Packet(new ApiTestMsg1() { TestMsg = "ApiServiceCall_Test1" }), false, isBackend);

            await apiReflections.CallMethod(routePacket.RouteHeader, routePacket.ToPacket(), apiSender);

            ReflectionTestResult.ResultMap["TestApiController_Test1"].Should().Be("ApiServiceCall_Test1");

            routePacket = RoutePacket.ApiOf(new Packet(new ApiTestMsg2() { TestMsg = "ApiServiceCall_Test2" }), false, isBackend);

            await apiReflections.CallMethod(routePacket.RouteHeader, routePacket.ToPacket(), apiSender);

            ReflectionTestResult.ResultMap["TestApiController_Test2"].Should().Be("ApiServiceCall_Test2");

        }

        [Fact]
        public async Task Test_CALL_Backend_Method()
        {
            var apiReflections = new ApiReflection();
            var apiSender = new Mock<IApiBackendSender>().Object;
            bool isBackend = false;

            var routePacket = RoutePacket.ApiOf(new Packet(new ApiTestMsg1() { TestMsg = "ApiBackendServiceCall_Test1" }), false, isBackend);

            await apiReflections.BackendCallMethod(routePacket.RouteHeader, routePacket.ToPacket(), apiSender);

            ReflectionTestResult.ResultMap["TestApiController_Test3"].Should().Be("ApiBackendServiceCall_Test1");

            routePacket = RoutePacket.ApiOf(new Packet(new ApiTestMsg2() { TestMsg = "ApiBackendServiceCall_Test2" }), false, isBackend);

            await apiReflections.BackendCallMethod(routePacket.RouteHeader, routePacket.ToPacket(), apiSender);

            ReflectionTestResult.ResultMap["TestApiController_Test4"].Should().Be("ApiBackendServiceCall_Test2");

        }

    }
}
