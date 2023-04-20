using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service.Play.Base
{
    public class ElementConfigurator
    {
        private readonly Dictionary<string, Func<IStageSender, IStage<IActor>>> _rooms = new();
        private readonly Dictionary<string, Func<IActorSender, IActor>> _users = new();

        public void Register(string stageType, Func<IStageSender, IStage<IActor>> stage, Func<IActorSender, IActor> actor)
        {
            _rooms[stageType] = stage;
            _users[stageType] = actor;
        }

        public IStage<IActor> GetStage(string stageType, IStageSender stageSender)
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

}
