using Playhouse.Protocol;

namespace PlayHouse.Production.Shared;

public class StageResult(ushort errorCode)
{
    public ushort ErrorCode { get; } = errorCode;

    public bool IsSuccess()
    {
        return ErrorCode == (short)BaseErrorCode.Success;
    }
}

public class CreateStageResult(ushort errorCode, IPacket createStageRes) : StageResult(errorCode)
{
    public IPacket CreateStageRes { get; } = createStageRes;
}

public class JoinStageResult(ushort errorCode, IPacket joinStageRes) : StageResult(errorCode)
{
    public IPacket JoinStageRes { get; } = joinStageRes;
}

public class CreateJoinStageResult(ushort errorCode, bool isCreate, IPacket createStageRes, IPacket joinStageRes)
    : StageResult(errorCode)
{
    public bool IsCreate { get; } = isCreate;
    public IPacket CreateStageRes { get; } = createStageRes;
    public IPacket JoinStageRes { get; } = joinStageRes;
}