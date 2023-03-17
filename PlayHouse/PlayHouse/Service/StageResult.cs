using PlayHouse.Communicator.Message;
using Playhouse.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service
{
    public class StageResult
    {
        public int ErrorCode { get; }

        public StageResult(int errorCode)
        {
            ErrorCode = errorCode;
        }

        public bool IsSuccess() => ErrorCode == (int)BaseErrorCode.Success;
    }

    public class CreateStageResult : StageResult
    {
        public long StageId { get; }
        public Packet CreateStageRes { get; }

        public CreateStageResult(int errorCode, long stageId, Packet createStageRes) : base(errorCode)
        {
            StageId = stageId;
            CreateStageRes = createStageRes;
        }
    }

    public class JoinStageResult : StageResult
    {
        public Packet JoinStageRes { get; }

        public JoinStageResult(int errorCode, Packet joinStageRes) : base(errorCode)
        {
            JoinStageRes = joinStageRes;
        }
    }

    public class CreateJoinStageResult : StageResult
    {
        public bool IsCreate { get; }
        public Packet CreateStageRes { get; }
        public Packet JoinStageRes { get; }

        public CreateJoinStageResult(int errorCode, bool isCreate, Packet createStageRes, Packet joinStageRes) : base(errorCode)
        {
            IsCreate = isCreate;
            CreateStageRes = createStageRes;
            JoinStageRes = joinStageRes;
        }
    }
}
