using Playhouse.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlayHouse.Production;

namespace PlayHouse.Service
{
    public class StageResult
    {
        public short ErrorCode { get; }

        public StageResult(short errorCode)
        {
            ErrorCode = errorCode;
        }

        public bool IsSuccess() => ErrorCode == (short)BaseErrorCode.Success;
    }

    public class CreateStageResult : StageResult
    {
        public Packet CreateStageRes { get; }

        public CreateStageResult(short errorCode, Packet createStageRes) : base(errorCode)
        {
            CreateStageRes = createStageRes;
        }
    }

    public class JoinStageResult : StageResult
    {
        public Packet JoinStageRes { get; }
        public int StageIndex { get; }

        public JoinStageResult(short errorCode,int stageIndex, Packet joinStageRes) : base(errorCode)
        {
            JoinStageRes = joinStageRes;
            StageIndex = stageIndex;
        }
    }

    public class CreateJoinStageResult : StageResult
    {
        public bool IsCreate { get; }
        public int StageIndex { get; }
        public Packet CreateStageRes { get; }
        public Packet JoinStageRes { get; }

        public CreateJoinStageResult(short errorCode, bool isCreate,int stageIndex, Packet createStageRes, Packet joinStageRes) : base(errorCode)
        {
            IsCreate = isCreate;
            CreateStageRes = createStageRes;
            JoinStageRes = joinStageRes;
            StageIndex = stageIndex;
        }
    }
}
