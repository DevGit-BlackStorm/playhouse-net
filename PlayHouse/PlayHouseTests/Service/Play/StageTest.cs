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

namespace PlayHouseTests.Service.Play
{
    //public class StageTest
    //{
    //    private readonly List<RoutePacket> resultList = new();
    //    private readonly string stageType = "dungeon";
    //    private PlayProcessor playProcessor;
    //    private readonly long testStageId = 10000L;
    //    private readonly string sessionEndpoint = "tcp://127.0.0.1:5555";
    //    private readonly string bindEndpoint = "tcp://127.0.0.1:8777";
    //    private BaseStage stage;
    //    private XStageSender xStageSender;
    //    private IStage<IActor> contentStage = Mock.Of<IStage<IActor>>(MockBehavior.Strict);
    //    private long stageId = 1;
    //    Mock<IClientCommunicator> clientCommunicator;

    //    public StageTest()
    //    {
    //        clientCommunicator = new Mock<IClientCommunicator>();
    //        var reqCache = new RequestCache(0);
    //        var playOption = new PlayOption();
    //        playOption.ElementConfigurator.Register(
    //            stageType,
    //            stageSender => contentStage,
    //            actorSender => Mock.Of<IActor>(MockBehavior.Strict)
    //        );
    //        var serverInfoCenter = Mock.Of<IServerInfoCenter>(MockBehavior.Strict);

    //        playProcessor = new PlayProcessor(
    //            2,
    //            bindEndpoint,
    //            playOption,
    //            clientCommunicator.Object,
    //            reqCache,
    //            Mock.Of<IServerInfoCenter>(MockBehavior.Strict)
    //        );
    //        xStageSender = new XStageSender(2, stageId, playProcessor, clientCommunicator.Object, reqCache);

    //        stage = new BaseStage(
    //            stageId,
    //            playProcessor,
    //            clientCommunicator.Object,
    //            reqCache,
    //            serverInfoCenter,
    //            xStageSender
    //        );

    //        Mock.Get(stage)
    //            .SetupPrivate("updateSessionRoomInfo", ItExpr.IsAny<string>(), ItExpr.IsAny<int>())
    //            .Returns(Task.FromResult(1));

    //        Mock.Get(contentStage)
    //            .Setup(stage => stage.OnCreate(It.IsAny<Packet>()))
    //            .Returns(Task.FromResult(new ReplyPacket(0, new TestMsg { TestMsg_ = "onCreate" })));

    //        Mock.Get(contentStage)
    //            .Setup(stage => stage.OnJoinStage(It.IsAny<IActor>(), It.IsAny<Packet>()))
    //            .Returns(Task.FromResult(new ReplyPacket(0, new TestMsg { TestMsg_ = "onJoinStage" })));
    //    }

    //    [Fact]
    //    public void CreateRoom_ShouldSucceed()
    //    {
    //        // given
    //        RoutePacket capturePacket ;
    //        clientCommunicator.Setup(x => x.Send(It.IsAny<string>(), It.IsAny<RoutePacket>()))
    //            .Callback<RoutePacket>(arg=>capturePacket = arg)
    //            ;

    //        // when
    //        stage.Send(CreateRoomPacket(stageType));

    //        // then
    //        var result = slotPacket;

    //        result.RouteHeader.Header.ErrorCode.Should().Be(Common.BaseErrorCode.SUCCESS.Number);

    //        result.MsgId().Should().Be(Server.CreateStageRes.Descriptor.Index);
    //        var createStageRes = Server.CreateStageRes.Parser.ParseFrom(result.Data());

    //        createStageRes.PayloadId.Should().Be(TestMsg.Descriptor.Index);

    //        TestMsg.Parser.ParseFrom(createStageRes.Payload).TestMsg_.Should().Be("onCreate");
    //    }

    //    [Fact]
    //    public void CreateRoom_WithInvalidType_ShouldReturnInvalidError()
    //    {
    //        // given
    //        var slotPacket = new RoutePacket();
    //        clientCommunicator.Setup(x => x.Send(It.IsAny<ServerInfo>(), ref slotPacket)).Verifiable();

    //        // when
    //        stage.Send(CreateRoomPacket("invalid type"));

    //        // then
    //        var result = slotPacket;

    //        result.RouteHeader.Header.ErrorCode.Should().Be(Common.BaseErrorCode.STAGE_TYPE_IS_INVALID.Number);
    //    }

    //    private RoutePacket CreateRoomPacket(string stageType)
    //    {
    //        var packet = new Packet(new CreateStageReq
    //        {
    //            StageType = stageType
    //        });

    //        var result = RoutePacket.StageOf(0, 0, packet, true, true);
    //        result.SetMsgSeq(1);
    //        return result;
    //    }

    //    private RoutePacket JoinRoomPacket(long stageId, long accountId)
    //    {
    //        var packet = new Packet(new JoinStageReq
    //        {
    //            SessionEndpoint = sessionEndpoint,
    //            Sid = 1,
    //            PayloadId = 2,
    //            Payload = ByteString.Empty
    //        });
    //        var result = RoutePacket.StageOf(stageId, accountId, packet, true, true);
    //        result.SetMsgSeq(2);
    //        return result;
    //    }

    //    private RoutePacket CreateJoinRoomPacket(string stageType, long stageId, long accountId)
    //    {
    //        var req = new CreateJoinStageReq
    //        {
    //            StageType = stageType,
    //            SessionEndpoint = sessionEndpoint,
    //            Sid = 1,
    //            CreatePayloadId = 1,
    //            CreatePayload = ByteString.Empty,
    //            JoinPayloadId = 2,
    //            JoinPayload = ByteString.Empty
    //        };
    //        var packet = new Packet (req);
    //        var result = RoutePacket.StageOf(stageId, accountId, packet, true, true);
    //        result.SetMsgSeq(3);
    //        return result;
    //    }

    //    private async Task CreateRoomWithSuccess()
    //    {
    //        var result = new List<RoutePacket>();
    //        clientCommunicator.Setup(c => c.Send(It.IsAny<string>(), It.IsAny<RoutePacket>())).Callback<RoutePacket>(arg => result.Add(arg));

    //        await stage.Send(CreateRoomPacket(stageType));


    //        result[0].RouteHeader.Header.ErrorCode.Should().Be((short)BaseErrorCode.Success);

    //        var createStageRes = CreateStageRes.Parser.ParseFrom(result[0].Data);
    //    }
    //}
}
