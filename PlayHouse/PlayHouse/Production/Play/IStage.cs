using PlayHouse.Production.Shared;

namespace PlayHouse.Production.Play;

public interface IStage
{
    public IStageSender StageSender { get; }

    public Task<(ushort errorCode, IPacket reply)> OnCreate(IPacket packet);
    public Task<(ushort errorCode, IPacket reply)> OnJoinStage(IActor actor, IPacket packet);
    public Task OnDispatch(IActor actor, IPacket packet);
    public Task OnDisconnect(IActor actor);
    public Task OnPostCreate();
    public Task OnPostJoinStage(IActor actor);
}

//public interface IStage<TA, TP> where TA : IActor where TP : IPacket
//{
//    public IStageSender StageSender { get; }

//    public Task<(ushort errorCode, IPacket reply)> OnCreate(IPacket packet);
//    public Task<(ushort errorCode, IPacket reply)> OnJoinStage(object actor, IPacket packet);
//    public Task OnDispatch(object actor, IPacket packet);
//    public Task OnDisconnect(object actor);
//    public Task OnPostCreate();
//    public Task OnPostJoinStage(object actor);

//    public TA CreateActor(IActor actor);
//    public TP CreatePacket(IPacket packet);
//}