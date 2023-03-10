using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse
{
    public interface ILogger
    {
        void Debug(string message, string className);
        void Info(string message, string className);
        void Warn(string message, string className);
        void Error(string message, string className, Exception? ex = null);
    }

    public  class ConsoleLogger : ILogger
    {
        private string GetTimeStamp()
        {
            DateTime now = DateTime.Now;
            string formattedTime = now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            return formattedTime;
        }
        public void Debug(string message, string className)
        {
            //[Timestamp] [Level] [SourceContext] [Message] [Exception]
            Console.WriteLine($"{GetTimeStamp()} DEBUG: ({className}) - {message} ");
        }

        public void Info(string message, string className)
        {
            Console.WriteLine($"{GetTimeStamp()} INFO: ({className}) - {message} ");
        }

        public void Warn(string message, string className)
{
            Console.WriteLine($"{GetTimeStamp()} WARN: ({className}) - {message} ");
        }

        public void Error(string message, string className, Exception? ex = null)
{
            if (ex != null)
            {
                Console.WriteLine($"{GetTimeStamp()} ERROR: ({className}) - {message} [{ex}]");
            }
            else
            {
                Console.WriteLine($"{GetTimeStamp()} ERROR: ({className}) - {message} ");

            }
        }
    }
}
