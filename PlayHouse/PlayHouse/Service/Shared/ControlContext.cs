using PlayHouse.Production.Shared;

namespace PlayHouse.Service.Shared
{
    public static class ControlContext
    {
        private static  ISender? _sender;
        private static  ISystemPanel? _systemPanel;
        public static void Init(ISender sender,ISystemPanel systemPanel)
        {
            _sender = sender;
            _systemPanel = systemPanel;
        }
        public static ISender Sender => _sender!;
        public static ISystemPanel SystemPanel => _systemPanel!;
    }

}
