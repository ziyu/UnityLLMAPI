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
    }
}
