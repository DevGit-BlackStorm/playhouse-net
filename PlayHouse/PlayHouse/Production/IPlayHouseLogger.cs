namespace PlayHouse.Production
{
    public interface IPlayHouseLogger
    {
        void Debug(Func<string> messageFactory, string className);
        void Info(Func<string> messageFactory, string className);
        void Warn(Func<string> messageFactory, string className);
        void Error(Func<string> messageFactory, string className);
        void Trace(Func<string> messageFactory, string className);
        void Fatal(Func<string> messageFactory, string className);
    }

    public enum LogLevel
    {
        Trace = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        Fatal = 5
    }

    public class ConsoleLogger : IPlayHouseLogger
    {
        private string GetTimeStamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }

        public void Trace(Func<string> messageFactory, string className)
        {
            Console.WriteLine($"{GetTimeStamp()} TRACE: ({className}) - {messageFactory()}");
        }

        public void Debug(Func<string> messageFactory, string className)
        {
            Console.WriteLine($"{GetTimeStamp()} DEBUG: ({className}) - {messageFactory()}");
        }

        public void Info(Func<string> messageFactory, string className)
        {
            Console.WriteLine($"{GetTimeStamp()} INFO: ({className}) - {messageFactory()}");
        }

        public void Warn(Func<string> messageFactory, string className)
        {
            Console.WriteLine($"{GetTimeStamp()} WARN: ({className}) - {messageFactory()}");
        }

        public void Error(Func<string> messageFactory, string className)
        {
            Console.WriteLine($"{GetTimeStamp()} ERROR: ({className}) - {messageFactory()}");
        }

        public void Fatal(Func<string> messageFactory, string className)
        {
            Console.WriteLine($"{GetTimeStamp()} FATAL: ({className}) - {messageFactory()}");
        }
    }

    public static class LOG
    {
        private static IPlayHouseLogger _logger = new ConsoleLogger();
        private static LogLevel _logLevel = LogLevel.Trace;

        public static void SetLogger(IPlayHouseLogger logger, LogLevel logLevel = LogLevel.Trace)
        {
            _logger = logger;
            _logLevel = logLevel;
        }

        public static void Trace(Func<string> messageFactory, Type clazz)
        {
            if (LogLevel.Trace >= _logLevel)
            {
                _logger.Trace(messageFactory, clazz.Name);
            }
        }

        public static void Debug(Func<string> messageFactory, Type clazz)
        {
            if (LogLevel.Debug >= _logLevel)
            {
                _logger.Debug(messageFactory, clazz.Name);
            }
        }

        public static void Info(Func<string> messageFactory, Type clazz)
        {
            if (LogLevel.Info >= _logLevel)
            {
                _logger.Info(messageFactory, clazz.Name);
            }
        }

        public static void Warn(Func<string> messageFactory, Type clazz)
        {
            if (LogLevel.Warning >= _logLevel)
            {
                _logger.Warn(messageFactory, clazz.Name);
            }
        }

        public static void Error(Func<string> messageFactory, Type clazz)
        {
            if (LogLevel.Error >= _logLevel)
            {
                _logger.Error(messageFactory, clazz.Name);
            }
        }

        // public static void Error(Func<string> messageFactory, Type clazz, Exception ex)
        // {
        //     if (LogLevel.Error >= _logLevel)
        //     {
        //         _logger.Error(messageFactory, clazz.Name, ex);
        //     }
        // }

        public static void Fatal(Func<string> messageFactory, Type clazz)
        {
            if (LogLevel.Fatal >= _logLevel)
            {
                _logger.Fatal(messageFactory, clazz.Name);
            }
        }
    }
}
