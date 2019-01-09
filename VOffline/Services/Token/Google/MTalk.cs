using System;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using log4net;
using VOffline.Models.Google;

namespace VOffline.Services.Google
{
    public class MTalk
    {
        public void SendRequest(GoogleCredentials googleCredentials, ILog log)
        {
            log.Debug($"mtalk build request");
            var request = BuildMTalkRequest(googleCredentials);
            log.Debug($"mtalk tcp connect");
            using (var tcpClient = new TcpClient(Host, Port))
            {
                using (var sslStream = new SslStream(tcpClient.GetStream(), false, (sender, certificate, chain, errors) => true, null))
                {
                    log.Debug($"mtalk ssl authenticate");
                    sslStream.AuthenticateAsClient(Host);
                    log.Debug($"mtalk send request");
                    sslStream.Write(request);
                    sslStream.Flush();
                    log.Debug($"mtalk read response");
                    sslStream.ReadByte();
                    var responseCode = sslStream.ReadByte();
                    if (responseCode != SuccessCode)
                    {
                        throw new InvalidOperationException($"MTalk expected response code [{SuccessCode}], got [{responseCode}]");
                    }
                }
            }
        }

        private byte[] BuildMTalkRequest(GoogleCredentials googleCredentials)
        {
            var idStringBytes = Encoding.ASCII.GetBytes(googleCredentials.Id.ToString());
            var idLen = VarInt.Write(idStringBytes.Length).ToList();
            
            var tokenStringByes = Encoding.ASCII.GetBytes(googleCredentials.Token.ToString());
            var tokenLen = VarInt.Write(tokenStringByes.Length).ToList();

            var hexId = "android-" + BitConverter.ToString(googleCredentials.RawId.ToArray()).Replace("-", string.Empty).ToLowerInvariant();
            var hexIdBytes = Encoding.ASCII.GetBytes(hexId);
            var hexIdLen = VarInt.Write(hexIdBytes.Length);

            var body = message1
                .Concat(idLen)
                .Concat(idStringBytes)
                .Concat(message2)
                .Concat(idLen)
                .Concat(idStringBytes)
                .Concat(message3)
                .Concat(tokenLen)
                .Concat(tokenStringByes)
                .Concat(message4)
                .Concat(hexIdLen)
                .Concat(hexIdBytes)
                .Concat(message5)
                .ToList();

            var bodyLen = VarInt.Write(body.Count);
            return message6
                .Concat(bodyLen)
                .Concat(body)
                .ToArray();
        }

        

        private byte[] message1 = { 0x0a, 0x0a, 0x61, 0x6e, 0x64, 0x72, 0x6f, 0x69, 0x64, 0x2d, 0x31, 0x39, 0x12, 0x0f, 0x6d, 0x63, 0x73, 0x2e, 0x61, 0x6e, 0x64, 0x72, 0x6f, 0x69, 0x64, 0x2e, 0x63, 0x6f, 0x6d, 0x1a };
        private byte[] message2 = { 0x22 };
        private byte[] message3 = { 0x2a };
        private byte[] message4 = { 0x32 };
        private byte[] message5 = { 0x42, 0x0b, 0x0a, 0x06, 0x6e, 0x65, 0x77, 0x5f, 0x76, 0x63, 0x12, 0x01, 0x31, 0x60, 0x00, 0x70, 0x01, 0x80, 0x01, 0x02, 0x88, 0x01, 0x01 };
        private byte[] message6 = { 0x29, 0x02 };

        private const int SuccessCode = 3;
        private const string Host = "mtalk.google.com";
        private const int Port = 5228;
    }
}
