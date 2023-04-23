using Docker.DotNet.Models;
using Moq.Protected;
using Moq;
using Org.Ulalax.Playhouse.Protocol;
using PlayHouse.Communicator.Message;
using PlayHouse.Service.Play.Base;
using PlayHouse.Service.Play;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using PlayHouse.Communicator;
using Google.Protobuf;
using Playhouse.Protocol;
using FluentAssertions;
using System.Runtime.CompilerServices;
using System.Reflection;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
namespace PlayHouseTests.Service.Play
{
   
    public class StageTest
    {
        private readonly List<RoutePacket> resultList = new();
        private readonly string stageType = "dungeon";
        private PlayProcessor playProcessor;
        private readonly long testStageId = 10000L;
        private readonly string sessionEndpoint = "tcp://127.0.0.1:5555";
        private readonly string bindEndpoint = "tcp://127.0.0.1:8777";
        private BaseStage stage;
        private XStageSender xStageSender;
        private IStage<IActor> contentStage = Mock.Of<IStage<IActor>>();
        private long stageId = 1;
        Mock<IClientCommunicator> clientCommunicator;

     
        public StageTest()
        {
            clientCommunicator = new Mock<IClientCommunicator>();
            var reqCache = new RequestCache(0);
            var playOption = new PlayOption();

            playOption.ElementConfigurator.Register(
                stageType,
                stageSender => contentStage,
                actorSender => Mock.Of<IActor>()
            );
            var serverInfoCenter = Mock.Of<IServerInfoCenter>();

            playProcessor = new PlayProcessor(
                2,
                bindEndpoint,
                playOption,
                clientCommunicator.Object,
                reqCache,
                Mock.Of<IServerInfoCenter>()
            );
            xStageSender = new XStageSender(2, stageId, playProcessor, clientCommunicator.Object, reqCache);

            Mock<ISessionUpdater> sessionUpdator = new Mock<ISessionUpdater>();

            sessionUpdator.Setup(updator=>updator.UpdateStageInfo(It.IsAny<string>(),It.IsAny<int>())).Returns(Task.FromResult(1));


            stage = new BaseStage(
                stageId,
                playProcessor,
                clientCommunicator.Object,
                reqCache,
                serverInfoCenter,
                sessionUpdator.Object,
                xStageSender
            );


    
            Mock.Get(contentStage)
                .Setup(stage => stage.OnCreate(It.IsAny<Packet>()))
                .Returns(Task.FromResult(new ReplyPacket(0, new TestMsg { TestMsg_ = "onCreate" })));

            Mock.Get(contentStage)
                .Setup(stage => stage.OnJoinStage(It.IsAny<IActor>(), It.IsAny<Packet>()))
                .Returns(Task.FromResult(new ReplyPacket(0, new TestMsg { TestMsg_ = "onJoinStage" })));
        }

        [Fact]
        public async Task CreateRoom_ShouldSucceed()
        {
            // given
            List<RoutePacket> result = new List<RoutePacket>();
            clientCommunicator.Setup(x => x.Send(It.IsAny<string>(), It.IsAny<RoutePacket>()))
            .Callback<string,RoutePacket>((sid,packet) => result.Add(packet));

            // when
            await stage.Send(CreateRoomPacket(stageType));

            // then
            result[0].RouteHeader.Header.ErrorCode.Should().Be((short)BaseErrorCode.Success);

            result[0].MsgId.Should().Be(CreateStageRes.Descriptor.Index);
            var createStageRes = CreateStageRes.Parser.ParseFrom(result[0].Data);

            createStageRes.PayloadId.Should().Be(TestMsg.Descriptor.Index);

            TestMsg.Parser.ParseFrom(createStageRes.Payload).TestMsg_.Should().Be("onCreate");
        }

        [Fact]
        public async Task CreateRoom_WithInvalidType_ShouldReturnInvalidError()
        {
            // given
            List<RoutePacket> result = new List<RoutePacket>();
            clientCommunicator.Setup(x => x.Send(It.IsAny<string>(), It.IsAny<RoutePacket>()))
            .Callback<string, RoutePacket>((sid, packet) => result.Add(packet));

            // when
            await stage.Send(CreateRoomPacket("invalid type"));

            // then
            result[0].RouteHeader.Header.ErrorCode.Should().Be((short)BaseErrorCode.StageTypeIsInvalid);
        }

        [Fact]
        public async Task CreateJoinRoomInCreateState_ShouldBeSuccess()
        {
            List<RoutePacket> result = new List<RoutePacket>();
            clientCommunicator.Setup(x => x.Send(It.IsAny<string>(), It.IsAny<RoutePacket>()))
            .Callback<string, RoutePacket>((sid, packet) => result.Add(packet));

            var createJoinRoom = CreateJoinRoomPacket(stageType, testStageId, 1000);
            await stage.Send(createJoinRoom);


            result[0].MsgId.Should().Be(CreateJoinStageRes.Descriptor.Index);
            var createJoinStageRes = CreateJoinStageRes.Parser.ParseFrom(result[0].Data);

            createJoinStageRes.IsCreated.Should().BeTrue();
            createJoinStageRes.CreatePayloadId.Should().Be(TestMsg.Descriptor.Index);
            createJoinStageRes.JoinPayloadId.Should().Be(TestMsg.Descriptor.Index);

            TestMsg.Parser.ParseFrom(createJoinStageRes.CreatePayload).TestMsg_.Should().Be("onCreate");
            TestMsg.Parser.ParseFrom(createJoinStageRes.JoinPayload).TestMsg_.Should().Be("onJoinStage");
        }

        [Fact]
        public async Task TestCreateJoinRoomInJoinState()
        {
            await CreateRoomWithSuccess();
            // Arrange
            List<RoutePacket> result = new List<RoutePacket>();
            clientCommunicator.Setup(x => x.Send(It.IsAny<string>(), It.IsAny<RoutePacket>()))
            .Callback<string, RoutePacket>((sid, packet) => result.Add(packet));

            var createJoinRoom = CreateJoinRoomPacket(stageType, stageId, 1000);
            // Act
            await stage.Send(createJoinRoom);

            // Assert
            CreateJoinStageRes.Descriptor.Index.Should().Be(result[0].MsgId);
            var createJoinStageRes = CreateJoinStageRes.Parser.ParseFrom(result[0].Data);

            createJoinStageRes.IsCreated.Should().BeFalse();
            createJoinStageRes.CreatePayloadId.Should().Be(0);
            createJoinStageRes.JoinPayloadId.Should().Be(TestMsg.Descriptor.Index);
        }

        [Fact]
        public async Task AsyncBlock_ShouldRunBlocking()
        {
            String result = "";
            await stage.Send(AsyncBlockPacket.Of(stageId, async pass => { result = (string)pass; await Task.CompletedTask; }, "test async block"));
            Assert.Equal("test async block", result);
        }

        private RoutePacket CreateRoomPacket(string stageType)
        {
            var packet = new Packet(new CreateStageReq
            {
                StageType = stageType
            });

            var result = RoutePacket.StageOf(0, 0, packet, true, true);
            result.SetMsgSeq(1);
            return result;
        }

        private RoutePacket JoinRoomPacket(long stageId, long accountId)
        {
            var packet = new Packet(new JoinStageReq
            {
                SessionEndpoint = sessionEndpoint,
                Sid = 1,
                PayloadId = 2,
                Payload = ByteString.Empty
            });
            var result = RoutePacket.StageOf(stageId, accountId, packet, true, true);
            result.SetMsgSeq(2);
            return result;
        }

        private RoutePacket CreateJoinRoomPacket(string stageType, long stageId, long accountId)
        {
            var req = new CreateJoinStageReq
            {
                StageType = stageType,
                SessionEndpoint = sessionEndpoint,
                Sid = 1,
                CreatePayloadId = 1,
                CreatePayload = ByteString.Empty,
                JoinPayloadId = 2,
                JoinPayload = ByteString.Empty
            };
            var packet = new Packet(req);
            var result = RoutePacket.StageOf(stageId, accountId, packet, true, true);
            result.SetMsgSeq(3);
            return result;
        }

        private async Task CreateRoomWithSuccess()
        {
            var result = new List<RoutePacket>();
            clientCommunicator.Setup(c => c.Send(It.IsAny<string>(), It.IsAny<RoutePacket>()))
            .Callback<string, RoutePacket>((sid, packet) => result.Add(packet));

            await stage.Send(CreateRoomPacket(stageType));


            result[0].RouteHeader.Header.ErrorCode.Should().Be((short)BaseErrorCode.Success);

            var createStageRes = CreateStageRes.Parser.ParseFrom(result[0].Data);
        }
    }
}
