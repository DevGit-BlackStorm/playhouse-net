using Playhouse.Protocol;
using PlayHouse.Production;

namespace PlayHouse.Service
{
    public class StageResult
    {
        public ushort ErrorCode { get; }

        public StageResult(ushort errorCode)
        {
            ErrorCode = errorCode;
        }

        public bool IsSuccess() => ErrorCode == (short)BaseErrorCode.Success;
    }

    public class CreateStageResult : StageResult
    {
        public Packet CreateStageRes { get; }

        public CreateStageResult(ushort errorCode, Packet createStageRes) : base(errorCode)
        {
            CreateStageRes = createStageRes;
        }
    }

    public class JoinStageResult : StageResult
    {
        public Packet JoinStageRes { get; }
        public int StageIndex { get; }

        public JoinStageResult(ushort errorCode,int stageIndex, Packet joinStageRes) : base(errorCode)
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

        public CreateJoinStageResult(ushort errorCode, bool isCreate,int stageIndex, Packet createStageRes, Packet joinStageRes) : base(errorCode)
        {
            IsCreate = isCreate;
            CreateStageRes = createStageRes;
            JoinStageRes = joinStageRes;
            StageIndex = stageIndex;
        }
    }
}
