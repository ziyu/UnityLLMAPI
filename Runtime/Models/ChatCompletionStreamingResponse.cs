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
        public ToolCall[] tool_calls;
    }

    [Serializable]
    public class ToolCallChunk
    {
        public string index;
        public ToolCallFunctionChunk function;
    }

    [Serializable]
    public class ToolCallFunctionChunk
    {
        public string name;
        public string arguments;
    }
}
