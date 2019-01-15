using System;

namespace VOffline.Models.Storage
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