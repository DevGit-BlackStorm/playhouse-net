using PlayHouse.Communicator;

namespace PlayHouse.Service.Session
{
    class AtomicEnumWrapper<TEnum> where TEnum : Enum
    {
        private int _value;

        public AtomicEnumWrapper(TEnum initialValue)
        {
            _value = Convert.ToInt32(initialValue);
        }

        public TEnum Value
        {
            get => (TEnum)Enum.ToObject(typeof(TEnum), _value);
            set => Interlocked.Exchange(ref _value, Convert.ToInt32(value));
        }
        //public void Set(TEnum value)
        //{
        //    int newIntValue = Convert.ToInt32(value);
        //    if (newIntValue != _value)
        //    {
        //        Interlocked.Exchange(ref _value, newIntValue);
        //    }
        //}
        public bool CompareAndSet(TEnum expectedValue, TEnum newValue)
        {
            int expectedIntValue = Convert.ToInt32(expectedValue);
            int newIntValue = Convert.ToInt32(newValue);
            return Interlocked.CompareExchange(ref _value, newIntValue, expectedIntValue) == expectedIntValue;
        }

        //public  ServerState Get()
        //{
        //    throw new NotImplementedException();
        //}
    }
}