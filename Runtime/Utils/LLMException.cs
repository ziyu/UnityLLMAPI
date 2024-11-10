using System;
using UnityLLMAPI.Models;

namespace UnityLLMAPI.Utils
{
    public class LLMException : Exception
    {
        public LLMException(string message) : base(message) { }
        public LLMException(string message, Exception innerException) : base(message, innerException) { }

        public override string ToString()
        {
            string result = $"{GetType()}: {Message}";
            if (InnerException != null)
            {
                result += $"\n--- Inner Exception ---\n{InnerException}";
            }
            return result;
        }
    }

    public class LLMConfigurationException : LLMException
    {
        public LLMConfigurationException(string message) : base(message) { }
    }

    public class LLMNetworkException : LLMException
    {
        public LLMNetworkException(string message) : base(message) { }
        public LLMNetworkException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class LLMResponseException : LLMException
    {
        public string ResponseContent { get; }
        public int? StatusCode { get; }
        public string ProviderName { get; }
        public string RawError { get; }

        public LLMResponseException(string message, string responseContent = null, int? statusCode = null, 
            string providerName = null, string rawError = null) 
            : base(message)
        {
            ResponseContent = responseContent;
            StatusCode = statusCode;
            ProviderName = providerName;
            RawError = rawError;
        }

        public static LLMResponseException FromErrorResponse(ErrorResponse error, string responseContent)
        {
            string message = error.message;
            if (error.metadata?.provider_name != null)
            {
                message = $"[{error.metadata.provider_name}] {message}";
            }

            return new LLMResponseException(
                message,
                responseContent,
                error.code,
                error.metadata?.provider_name,
                error.metadata?.raw
            );
        }

        public override string ToString()
        {
            string result = base.ToString();
            if (StatusCode.HasValue)
                result += $"\nStatus Code: {StatusCode}";
            if (!string.IsNullOrEmpty(ProviderName))
                result += $"\nProvider: {ProviderName}";
            if (!string.IsNullOrEmpty(ResponseContent))
                result += $"\nResponse Content: {ResponseContent}";
            if (!string.IsNullOrEmpty(RawError))
                result += $"\nRaw Error: {RawError}";
            return result;
        }
    }

    public class LLMToolException : LLMException
    {
        public string ToolName { get; }

        public LLMToolException(string message, string toolName) 
            : base(message)
        {
            ToolName = toolName;
        }

        public override string ToString()
        {
            return $"{base.ToString()}\nTool Name: {ToolName}";
        }
    }

    public class LLMValidationException : LLMException
    {
        public string ParameterName { get; }
        
        public LLMValidationException(string message, string parameterName) 
            : base(message)
        {
            ParameterName = parameterName;
        }

        public override string ToString()
        {
            return $"{base.ToString()}\nParameter Name: {ParameterName}";
        }
    }
}
