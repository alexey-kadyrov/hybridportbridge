using System;
using System.Runtime.Serialization;

namespace DocaLabs.HybridPortBridge.Config
{
    [Serializable]
    public class ConfigurationErrorException : Exception
    {
        public ConfigurationErrorException()
        {
        }

        public ConfigurationErrorException(string message) : base(message)
        {
        }

        public ConfigurationErrorException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ConfigurationErrorException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
