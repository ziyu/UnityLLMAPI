using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityLLMAPI.Models;

namespace UnityLLMAPI.Config
{
    /// <summary>
    /// Configuration for ChatbotService
    /// </summary>
    [Serializable]
    public class ChatbotConfig
    {
        /// <summary>
        /// System prompt to initialize the conversation
        /// </summary>
        public string systemPrompt;

        /// <summary>
        /// Tool set for function calling capabilities
        /// </summary>
        public ToolSet toolSet;

        /// <summary>
        /// Whether to use streaming mode
        /// </summary>
        public bool useStreaming;

        /// <summary>
        /// Callback for streaming chunks (required if useStreaming is true)
        /// </summary>
        public Action<ChatMessage> onStreamingChunk;

        /// <summary>
        /// Async callback to decide whether to execute a tool call
        /// </summary>
        public Func<ChatMessageInfo,ToolCall, Task<bool>> shouldExecuteTool;

        /// <summary>
        /// Message to use when a tool call is skipped
        /// </summary>
        public string skipToolMessage = "Tool execution skipped by user";

        /// <summary>
        /// Validate the configuration
        /// </summary>
        public void ValidateConfig()
        {
            if (useStreaming && onStreamingChunk == null)
            {
                throw new ArgumentException("Streaming chunk callback is required when streaming is enabled", nameof(onStreamingChunk));
            }

            if (string.IsNullOrEmpty(skipToolMessage))
            {
                throw new ArgumentException("Skip tool message cannot be empty", nameof(skipToolMessage));
            }
        }
    }
}
