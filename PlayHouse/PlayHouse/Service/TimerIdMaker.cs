using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse.Service
{
    public static class TimerIdMaker
    {
        private static long timerIds = 0;

        public static long MakeId()
        {
            return Interlocked.Increment(ref timerIds);
        }
    }
}
