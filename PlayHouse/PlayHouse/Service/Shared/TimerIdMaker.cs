namespace PlayHouse.Service.Shared
{
    public static class TimerIdMaker
    {
        private static long _timerIds = 0;

        public static long MakeId()
        {
            return Interlocked.Increment(ref _timerIds);
        }
    }
}
