using Playhouse.Protocol;

namespace PlayHouse.Production.Shared
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
        public IPacket CreateStageRes { get; }

        public CreateStageResult(ushort errorCode, IPacket createStageRes) : base(errorCode)
        {
            CreateStageRes = createStageRes;
        }
    }

    public class JoinStageResult : StageResult
    {
        public IPacket JoinStageRes { get; }

        public JoinStageResult(ushort errorCode,  IPacket joinStageRes) : base(errorCode)
        {
            JoinStageRes = joinStageRes;
        }
    }

    public class CreateJoinStageResult : StageResult
    {
        public bool IsCreate { get; }
        public IPacket CreateStageRes { get; }
        public IPacket JoinStageRes { get; }

        public CreateJoinStageResult(ushort errorCode, bool isCreate,  IPacket createStageRes, IPacket joinStageRes) : base(errorCode)
        {
            IsCreate = isCreate;
            CreateStageRes = createStageRes;
            JoinStageRes = joinStageRes;
        }
    }
}
