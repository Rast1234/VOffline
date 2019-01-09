using System;

namespace VOffline.Services.Handlers
{
    public class NetworkException : ApplicationException
    {
        public NetworkException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public NetworkException(string message) : base(message)
        {
        }
    }
}