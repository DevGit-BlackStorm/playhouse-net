namespace PlayHouse.Production.Play
{
    public interface IStage
    {
        public IStageSender StageSender { get; }

        public Task<(ushort errorCode,IPacket reply)> OnCreate(IPacket packet);
        public Task<(ushort errorCode, IPacket reply)> OnJoinStage(object actor, IPacket packet);
        public Task OnDispatch(object actor, IPacket packet);
        public Task OnDisconnect(object actor);
        public Task OnPostCreate();
        public Task OnPostJoinStage(object actor);
    }
}
