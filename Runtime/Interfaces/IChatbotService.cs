using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityLLMAPI.Models;

namespace UnityLLMAPI.Interfaces
{
    /// <summary>
    /// Interface for chatbot services that handle conversations and tool interactions
    /// </summary>
    public interface IChatbotService
    {
        /// <summary>
        /// 状态变更事件
        /// </summary>
        event EventHandler<ChatStateChangedEventArgs> StateChanged;

        /// <summary>
        /// Get all messages in the conversation history
        /// </summary>
        IReadOnlyList<ChatMessage> Messages { get; }

        /// <summary>
        /// 获取当前会话状态
        /// </summary>
        ChatState CurrentState { get; }
        
        /// <summary>
        /// 是否发送和处理消息中
        /// </summary>
        bool IsPending { get; }

        /// <summary>
        /// 是否处于中断状态
        /// </summary>
        bool IsInterrupted { get; }


        /// <summary>
        /// 获取指定消息的状态
        /// </summary>
        /// <param name="messageId">消息ID</param>
        /// <returns>消息状态</returns>
        ChatMessageState GetMessageState(string messageId);

        /// <summary>
        /// 获取所有pending状态的消息
        /// </summary>
        /// <returns>pending状态的消息列表</returns>
        IReadOnlyList<ChatMessageInfo> GetPendingMessages();

        /// <summary>
        /// Create a system message with the given prompt
        /// </summary>
        /// <param name="systemPrompt">Optional system prompt override</param>
        /// <returns>Created system message</returns>
        ChatMessage CreateSystemMessage(string systemPrompt = null);

        /// <summary>
        /// Add a system message to the conversation
        /// </summary>
        /// <param name="systemPrompt">Optional system prompt override</param>
        void AddSystemMessage(string systemPrompt = null);

        /// <summary>
        /// Add a message to the conversation
        /// </summary>
        /// <param name="message">Message to add</param>
        void AddMessage(ChatMessage message);

        /// <summary>
        /// Add a user message and get the assistant's response
        /// </summary>
        /// <param name="message">User message content</param>
        /// <returns>Assistant's response message</returns>
        Task<ChatMessage> SendMessage(string message, ChatParams @params=default);


        /// <summary>
        /// 尝试恢复中断的会话
        /// </summary>
        /// <returns>是否成功恢复</returns>
        Task<ChatMessage> ResumeSession(ChatParams @params=default);

        /// <summary>
        /// Clear conversation history
        /// </summary>
        /// <param name="keepSystemMessage">Whether to keep the system message</param>
        void ClearHistory(bool keepSystemMessage = true);
    }

    public struct ChatParams
    {
        public string Model;
        public CancellationToken CancellationToken;
    }
}
