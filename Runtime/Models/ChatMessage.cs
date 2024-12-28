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
        public static class Roles
        {
            public static readonly string System = "system";
            public static readonly string User = "user";
            public static readonly string Assistant = "assistant";
            public static readonly string Tool = "tool";
        }
        
        /// <summary>
        /// Role of the message sender (system, user, assistant, tool)
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
        /// ID of the tool being responded to
        /// </summary>
        public string tool_call_id;

        /// <summary>
        /// Name of the tool being responded to
        /// </summary>
        public string name;

        public ChatMessage()
        {
        }

        public ChatMessage(string role, string content)
        {
            if (string.IsNullOrEmpty(role))
            {
                throw new ArgumentException("Role cannot be empty", nameof(role));
            }
            if (content == null) // Allow empty content for function calls
            {
                throw new ArgumentException("Content cannot be null", nameof(content));
            }

            this.role = role;
            this.content = content;
        }

        /// <summary>
        /// Create a tool response message
        /// </summary>
        public static ChatMessage CreateToolResponse(string toolCallId, string name, string content)
        {
            if (string.IsNullOrEmpty(toolCallId))
            {
                throw new ArgumentException("Tool call ID cannot be empty", nameof(toolCallId));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Tool name cannot be empty", nameof(name));
            }

            return new ChatMessage(Roles.Tool, content)
            {
                tool_call_id = toolCallId,
                name = name
            };
        }
        
        /// <summary>
        /// Create a system message
        /// </summary>
        public static ChatMessage CreateSystemMessage(string content)
        {
            return new ChatMessage(Roles.System, content);
        }

        /// <summary>
        /// Create a user message
        /// </summary>
        public static ChatMessage CreateUserMessage(string content)
        {
            return new ChatMessage(Roles.User, content);
        }

        /// <summary>
        /// Create an assistant message
        /// </summary>
        public static ChatMessage CreateAssistantMessage(string content)
        {
            return new ChatMessage(Roles.Assistant, content);
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
        public ErrorResponse error;
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
