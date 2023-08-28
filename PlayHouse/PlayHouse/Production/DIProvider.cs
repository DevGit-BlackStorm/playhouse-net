using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Production
{
    using System;
    using Microsoft.Extensions.DependencyInjection;

    public static class PlayServiceCollection
    {
        private static IServiceCollection? _instance;

        public static IServiceCollection Instance
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException("ServiceCollection has not been set.");
                return _instance;
            }
            set
            {
                if (_instance != null)
                    throw new InvalidOperationException("ServiceCollection has already been set.");
                _instance = value;
            }
        }
    }

    public static class PlayServiceProvider
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
