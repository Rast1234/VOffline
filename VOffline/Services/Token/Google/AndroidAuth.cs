using log4net;
using VOffline.Models.Google;

namespace VOffline.Services.Google
{
    public class AndroidAuth
    {
        private FileCache<GoogleCredentials> CredentialsCache { get; }
        private MTalk MTalk { get; }
        private GoogleHttpRequests GoogleHttpRequests { get; }

        public AndroidAuth(FileCache<GoogleCredentials> credentialsCache, MTalk mTalk, GoogleHttpRequests googleHttpRequests)
        {
            CredentialsCache = credentialsCache;
            MTalk = mTalk;
            GoogleHttpRequests = googleHttpRequests;
        }

        public GoogleCredentials GetCredentials(ILog log)
        {
            if (CredentialsCache.Value != null)
            {
                return CredentialsCache.Value;
            }

            var protobuf = GoogleHttpRequests.GetCheckIn(log);
            log.Debug($"parse protobuf response");
            var googleCredentials = new ProtobufParser(protobuf).Parse();
            MTalk.SendRequest(googleCredentials, log);
            log.Info($"success! {googleCredentials}");
            CredentialsCache.Value = googleCredentials;
            return CredentialsCache.Value;
        }
    }
}
