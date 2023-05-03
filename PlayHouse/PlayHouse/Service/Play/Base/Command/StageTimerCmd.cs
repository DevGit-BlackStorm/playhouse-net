﻿using PlayHouse.Communicator.Message;
using PlayHouse.Production;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service.Play.Base.Command
{
    public class StageTimerCmd : IBaseStageCmd
    {
        private readonly PlayProcessor _playProcessor;
        public PlayProcessor PlayProcessor => _playProcessor;

        public StageTimerCmd(PlayProcessor playProcessor)
        {
            _playProcessor = playProcessor;
        }

        public async Task Execute(BaseStage baseStage, RoutePacket routePacket)
        {
            var timerCallback = routePacket.TimerCallback;
            var timerId = routePacket.TimerId;
            if (baseStage.HasTimer(timerId))
            {
               var task  = (Task)timerCallback!.Invoke();
               await task.ConfigureAwait(false);
            }
            else
            {
                LOG.Warn($"timer already canceled stageId:{baseStage.StageId}, timerId:{timerId}", this.GetType());
            }
        }
    }

}
