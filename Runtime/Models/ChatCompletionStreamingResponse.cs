using System;

namespace UnityLLMAPI.Models
{
    [Serializable]
    public class ChatCompletionChunkResponse
    {
        public string id;
        public string @object;
        public long created;
        public string model;
        public ChatCompletionChunkChoice[] choices;
        public ErrorResponse error;
    }

    [Serializable]
    public class ErrorResponse
    {
        public string message;
        public int code;
        public ErrorMetadata metadata;
    }

    [Serializable]
    public class ErrorMetadata
    {
        public string raw;
        public string provider_name;
    }

    [Serializable]
    public class ChatCompletionChunkChoice
    {
        public ChatCompletionChunkDelta delta;
        public int index;
        public string finish_reason;
    }

    [Serializable]
    public class ChatCompletionChunkDelta
    {
        public string role;
        public string content;
        public ToolCallChunk[] tool_calls;
    }

    [Serializable]
    public class ToolCallChunk
    {
        public string id;
        public string type = "function";
        public ToolCallFunctionChunk function;
    }

    [Serializable]
    public class ToolCallFunctionChunk
    {
        public string name;
        public string arguments;
    }
}
