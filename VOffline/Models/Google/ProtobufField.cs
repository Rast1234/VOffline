namespace VOffline.Models.Google
{
    public class ProtobufField
    {
        public ProtobufField(int value)
        {
            Type = value & 0x7;  // last three bits, 0b00000111
            FieldNumber = value >> 3;
        }

        public int Type { get; }
        public int FieldNumber { get; }
    }
}