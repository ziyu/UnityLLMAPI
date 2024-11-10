using System;

namespace UnityLLMAPI.Utils.Json
{
    public class JsonException : Exception
    {
        public JsonException(string message) : base(message)
        {
        }

        public JsonException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
