using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityLLMAPI.Models;

namespace UnityLLMAPI.Interfaces
{
    /// <summary>
    /// Interface for chatbot services
    /// </summary>
    public interface IChatbotService
    {
        /// <summary>
        /// Get all messages in the conversation history
        /// </summary>
        IReadOnlyList<ChatMessage> Messages { get; }

        /// <summary>
        /// Add a message to the conversation
        /// </summary>
        /// <param name="message">Message</param>
        void AddMessage(ChatMessage message);

        /// <summary>
        /// Add a user message and get the assistant's response
        /// </summary>
        /// <param name="message">User message content</param>
        /// <param name="model">Optional model override</param>
        /// <returns>Assistant's response message</returns>
        Task<ChatMessage> SendMessage(string message, string model = null);


        /// <summary>
        /// Clear Message History
        /// </summary>
        /// <param name="keepSystemMessage"></param>
        void ClearHistory(bool keepSystemMessage=true);
    }
}
