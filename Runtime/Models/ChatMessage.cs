using System;
using UnityEngine;

namespace UnityLLMAPI.Models
{
    /// <summary>
    /// Represents a chat message in the conversation
    /// </summary>
    [Serializable]
    public class ChatMessage
    {
        /// <summary>
        /// Role of the message sender (system, user, assistant)
        /// </summary>
        public string role;

        /// <summary>
        /// Content of the message
        /// </summary>
        public string content;

        /// <summary>
        /// Tool calls made by the assistant
        /// </summary>
        public ToolCall[] tool_calls;

        /// <summary>
        /// Name of the tool being responded to
        /// </summary>
        public string tool_call_id;

        /// <summary>
        /// Name of the tool being responded to
        /// </summary>
        public string name;

        public ChatMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }

        /// <summary>
        /// Create a tool response message
        /// </summary>
        public static ChatMessage CreateToolResponse(string toolCallId, string name, string content)
        {
            return new ChatMessage("tool", content)
            {
                tool_call_id = toolCallId,
                name = name
            };
        }
    }

    /// <summary>
    /// Chat completion request model
    /// </summary>
    [Serializable]
    public class ChatCompletionRequest
    {
        public string model;
        public ChatMessage[] messages;
        public float temperature = 0.7f;
        public int max_tokens = 1000;
        public bool stream = false;
        public Tool[] tools;
        public string tool_choice = "auto";
    }

    /// <summary>
    /// Chat completion response model
    /// </summary>
    [Serializable]
    public class ChatCompletionResponse
    {
        public string id;
        public string object_type;
        public long created;
        public ChatChoice[] choices;
        public Usage usage;
    }

    [Serializable]
    public class ChatChoice
    {
        public ChatMessage message;
        public string finish_reason;
        public int index;
    }

    [Serializable]
    public class Usage
    {
        public int prompt_tokens;
        public int completion_tokens;
        public int total_tokens;
    }
}
