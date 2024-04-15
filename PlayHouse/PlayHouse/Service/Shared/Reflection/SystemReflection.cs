using PlayHouse.Communicator.Message;
using PlayHouse.Production.Api.Aspectify;
using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Shared.Reflection
{
    internal class SystemReflection
    {
        private readonly SystemHandleReflectionInvoker _reflectionInvoker;

        public SystemReflection(IServiceProvider serviceProvider)
        {
            _reflectionInvoker = new SystemHandleReflectionInvoker(serviceProvider,new List<AspectifyAttribute>());
        }

        public async Task CallMethodAsync( IPacket packet,ISystemPanel panenl, ISender sender)
        {
            
            string msgId = packet.MsgId;                
            await _reflectionInvoker.InvokeMethods(msgId, new object[] {packet,panenl,sender });
    }
    }
}
