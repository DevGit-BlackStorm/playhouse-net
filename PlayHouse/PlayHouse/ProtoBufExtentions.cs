using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlayHouse
{
    public static class ProtobufExtensions
    {
        public static T ParseFrom<T>(this MessageParser<T> parser, (byte[] data, int length) inputs) where T : IMessage<T>, new()
        {
            var (buffer, length) = inputs;
            return parser.ParseFrom(buffer, 0, length);
        }
    }
}
