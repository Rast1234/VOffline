using System;
using System.Collections.Generic;
using System.Linq;
using VOffline.Models.Google;

namespace VOffline.Services.Token.Google
{
    public class ProtobufParser
    {
        private const int IdFieldNumber = 7;
        private const int TokenFieldNumber = 8;

        private readonly byte[] data;
        private int offset;

        public ProtobufParser(byte[] data)
        {
            this.data = data;
            this.offset = 0;
        }

        public GoogleCredentials Parse()
        {
            List<byte> rawId = null;
            long? id = null;
            long? token = null;
            while (offset < data.Length)
            {
                var (value, length) = VarInt.Read(data, offset);
                offset += length;
                var field = new ProtobufField(value);
                switch (field.Type)
                {
                    case 0:
                        var (_, i) = VarInt.Read(data, offset);
                        offset += i;
                        break;
                    case 1 when field.FieldNumber == IdFieldNumber:
                        rawId = data.Skip(offset).Take(8).ToList();
                        id = BitConverter.ToInt64(data, offset);
                        offset += 8;
                        break;
                    case 1 when field.FieldNumber == TokenFieldNumber:
                        token = BitConverter.ToInt64(data, offset);
                        offset += 8;
                        break;
                    case 1:
                        offset += 8;
                        break;
                    case 2:
                        var (skip, j) = VarInt.Read(data, offset);
                        offset += skip + j;
                        break;
                    default:
                        throw new InvalidOperationException($"{nameof(ProtobufParser)} unexpected code [{field.Type}]");
                }
            }

            if (offset == data.Length && id == null && token == null)
            {
                throw new InvalidOperationException($"{nameof(ProtobufParser)} reached end of data, id and token not found");
            }

            if (id == null)
            {
                throw new InvalidOperationException($"{nameof(ProtobufParser)} id not found");
            }

            if (token == null)
            {
                throw new InvalidOperationException($"{nameof(ProtobufParser)}  token not found");
            }

            return new GoogleCredentials()
            {
                Id = id.Value,
                Token = token.Value,
                RawId = rawId
            };
        }

        
    }
}