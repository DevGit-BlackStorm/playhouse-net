
namespace PlayHouse.Production
{
    using System;

    public static class XServiceProvider
    {
        private static IServiceProvider? _instance;

        public static IServiceProvider Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("ServiceProvider has not been set.");
                return _instance;
            }
            set
            {
                if (_instance != null)
                    throw new InvalidOperationException("ServiceProvider has already been set.");
                _instance = value;
            }
        }
    }


}
