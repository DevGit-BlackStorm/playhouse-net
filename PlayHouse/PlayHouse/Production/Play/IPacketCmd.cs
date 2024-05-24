using PlayHouse.Production.Shared;

namespace PlayHouse.Production.Play;

public interface IPacketCmd<in TS, in TA> where TA : IActor
{
    public Task Execute(TS stage, TA actor, IPacket packet);
}