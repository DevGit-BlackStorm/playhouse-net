using FluentAssertions;
using Google.Protobuf.Collections;
using Moq;
using Org.Ulalax.Playhouse.Protocol;
using Playhouse.Protocol;
using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Service;
using PlayHouse.Service.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace PlayHouseTests.Service.Api.plain
{
    class ReflectionTestResult
    {
        public static Dictionary<string, string> ResultMap = new();
    }

    public  class ApiReflectionTest
    {
        [Fact]
        public async Task Test_Init_Method()
        {
            var apiReflections = new ApiReflection();
            var systemPanel = new Mock<ISystemPanel>();
            var sender = new Mock<ISender>();


            await apiReflections.CallInitMethod(systemPanel.Object, sender.Object);

            ReflectionTestResult.ResultMap["TestApiService_Init"].Should().Be("OK");
            ReflectionTestResult.ResultMap["TestApiBackendService_Init"].Should().Be("OK");

        }

        [Fact] public async Task Test_CALL_Method()
        {
            var apiReflections = new ApiReflection();
            var systemPanel = new Mock<ISystemPanel>();
            var sender = new Mock<ISender>();
            var apiSender = new AllApiSender(0, new Mock<IClientCommunicator>().Object, new RequestCache(0));
            bool isBackend = false;

            await apiReflections.CallInitMethod(systemPanel.Object, sender.Object);

            var routePacket = RoutePacket.ApiOf(new Packet(new ApiTestMsg1() { TestMsg = "ApiServiceCall_Test1" }), false, isBackend);

            await apiReflections.CallMethod(routePacket.RouteHeader, routePacket.ToPacket(), isBackend, apiSender);

            ReflectionTestResult.ResultMap["TestApiService_Test1"].Should().Be("ApiServiceCall_Test1");

            routePacket = RoutePacket.ApiOf(new Packet(new ApiTestMsg2() { TestMsg = "ApiServiceCall_Test2" }), false, isBackend);

            await apiReflections.CallMethod(routePacket.RouteHeader, routePacket.ToPacket(), isBackend, apiSender);

            ReflectionTestResult.ResultMap["TestApiService_Test2"].Should().Be("ApiServiceCall_Test2");

        }

        [Fact]
        public async Task Test_CALL_Backend_Method()
        {
            var apiReflections = new ApiReflection();
            var systemPanel = new Mock<ISystemPanel>();
            var sender = new Mock<ISender>();
            var apiSender = new AllApiSender(0, new Mock<IClientCommunicator>().Object, new RequestCache(0));
            bool isBackend = true;

            await apiReflections.CallInitMethod(systemPanel.Object, sender.Object);

            var routePacket = RoutePacket.ApiOf(new Packet(new ApiTestMsg1() { TestMsg = "ApiBackendServiceCall_Test1" }), false, isBackend);

            await apiReflections.CallMethod(routePacket.RouteHeader, routePacket.ToPacket(), isBackend, apiSender);

            ReflectionTestResult.ResultMap["TestApiBackendService_Test1"].Should().Be("ApiBackendServiceCall_Test1");

            routePacket = RoutePacket.ApiOf(new Packet(new ApiTestMsg2() { TestMsg = "ApiBackendServiceCall_Test2" }), false, isBackend);

            await apiReflections.CallMethod(routePacket.RouteHeader, routePacket.ToPacket(), isBackend, apiSender);

            ReflectionTestResult.ResultMap["TestApiBackendService_Test2"].Should().Be("ApiBackendServiceCall_Test2");

        }

    }
}
