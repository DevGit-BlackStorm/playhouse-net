namespace PlayHouse.Production.Play
{
    public interface IStage
    {
        public IStageSender StageSender { get; }

        public Task<ReplyPacket> OnCreate(IPacket packet);
        public Task<ReplyPacket> OnJoinStage(object actor, IPacket packet);
        public Task OnDispatch(object actor, IPacket packet);
        public Task OnDisconnect(object actor);
        public Task OnPostCreate();
        public Task OnPostJoinStage(object actor);
    }
}
