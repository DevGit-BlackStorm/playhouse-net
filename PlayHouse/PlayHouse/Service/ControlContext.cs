using PlayHouse.Production;

namespace PlayHouse.Service
{
    public static class ControlContext
    {
        public static ISender? BaseSender { get; set; }
        public static ISystemPanel? SystemPanel { get; set; }
    }

}
