using PlayHouse.Production.Shared;
using PlayHouse.Service.Play;

namespace PlayHouse.Production.Play;

public class PlayProducer
{
    private readonly Dictionary<string, Func<IStageSender, IStage>> _rooms = new();
    private readonly Dictionary<string, Func<IActorSender, IActor>> _users = new();

    public void Register(string stageType, Func<IStageSender, IStage> stage, Func<IActorSender, IActor> actor)
    {
        _rooms[stageType] = stage;
        _users[stageType] = actor;
    }

    public IStage GetStage(string stageType, IStageSender stageSender)
    {
        if (_rooms.TryGetValue(stageType, out var factory))
        {
            return factory(stageSender);
        }

        throw new KeyNotFoundException($"Stage type {stageType} not registered");
    }

    public IActor GetActor(string stageType, IActorSender actorSender)
    {
        if (_users.TryGetValue(stageType, out var factory))
        {
            return factory(actorSender);
        }

        throw new KeyNotFoundException($"Actor type {stageType} not registered");
    }

    internal bool IsInvalidType(string stageType)
    {
        return _rooms.ContainsKey(stageType);
    }
}