using FluentAssertions;
using Google.Protobuf.Collections;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Org.Ulalax.Playhouse.Protocol;
using Playhouse.Protocol;
using PlayHouse.Communicator;
using PlayHouse.Communicator.Message;
using PlayHouse.Production;
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
        static ApiReflectionTest()
        {
            PlayServiceCollection.Instance = new ServiceCollection();
            PlayServiceCollection.Instance.AddTransient<TestApiService>();
            var mockSender = new Mock<ISender>();
            PlayServiceCollection.Instance.AddSingleton<ISender>(ServiceProvider =>
            {
                return mockSender.Object;
            });
            var mockPanel = new Mock<ISystemPanel>();
            PlayServiceCollection.Instance.AddSingleton<ISystemPanel>(ServiceProvider =>
            {
                return mockPanel.Object;
            });

            PlayServiceProvider.Instance = PlayServiceCollection.Instance.BuildServiceProvider();

        }

        [Fact] public async Task Test_CALL_Method()
        {
            var apiReflections = new ApiReflection();
            var systemPanel = new Mock<ISystemPanel>();
            var sender = new Mock<ISender>();
            var apiSender = new AllApiSender(0, new Mock<IClientCommunicator>().Object, new RequestCache(0));
            bool isBackend = false;

            //await apiReflections.CallInitMethod(systemPanel.Object, sender.Object);

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

            // await apiReflections.CallInitMethod(systemPanel.Object, sender.Object);

            var routePacket = RoutePacket.ApiOf(new Packet(new ApiTestMsg1() { TestMsg = "ApiBackendServiceCall_Test1" }), false, isBackend);

            await apiReflections.CallMethod(routePacket.RouteHeader, routePacket.ToPacket(), isBackend, apiSender);

            ReflectionTestResult.ResultMap["TestApiService_Test3"].Should().Be("ApiBackendServiceCall_Test1");

            routePacket = RoutePacket.ApiOf(new Packet(new ApiTestMsg2() { TestMsg = "ApiBackendServiceCall_Test2" }), false, isBackend);

            await apiReflections.CallMethod(routePacket.RouteHeader, routePacket.ToPacket(), isBackend, apiSender);

            ReflectionTestResult.ResultMap["TestApiService_Test4"].Should().Be("ApiBackendServiceCall_Test2");

        }

    
    }
}
