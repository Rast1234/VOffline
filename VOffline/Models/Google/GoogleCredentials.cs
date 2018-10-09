using System.Collections.Generic;

namespace VOffline.Models.Google
{
    public class GoogleCredentials
    {
        public long Id { get; set; }
        public long Token { get; set; }
        public List<byte> RawId { get; set; }

        public override string ToString()
        {
            return $"{nameof(GoogleCredentials)}(id {Id}; token {Token})";
        }
    }
}