using System;
using System.Collections.Generic;

namespace VOffline.Services.Token.Google
{
    public static class VarInt
    {
        public static (int value, int length) Read(byte[] data, int offset)
        {
            var i = 0;
            var result = 0;
            while (i + offset < data.Length)
            {
                var current = data[i + offset];
                if ((current & 0x80) != 0)
                {
                    result |= (current ^ 0x80) << (i * 7);
                    i++;
                }
                else
                {
                    result |= current << (i * 7);
                    i++;
                    break;
                }
            }

            if (i + offset == data.Length)
            {
                throw new InvalidOperationException($"{nameof(VarInt)} failed to read varint");
            }

            return (result, i);
        }

        public static IEnumerable<byte> Write(int value)
        {
            while (value != 0)
            {
                var current = value & 0x7F;
                value >>= 7;
                if (value != 0)
                {
                    yield return (byte)(current | 0x80);
                }
                else
                {
                    yield return (byte)current;
                }
            }
        }
    }
}