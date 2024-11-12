using System;

namespace UnityLLMAPI.Models
{
    /// <summary>
    /// 聊天状态变更事件参数
    /// </summary>
    public class ChatStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 会话ID
        /// </summary>
        public string SessionId { get; }

        /// <summary>
        /// 消息ID（如果状态变更与特定消息相关）
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        /// 新的消息状态（如果状态变更与特定消息相关）
        /// </summary>
        public ChatMessageState NewMessageState { get; }

        /// <summary>
        /// 旧的消息状态（如果状态变更与特定消息相关）
        /// </summary>
        public ChatMessageState OldMessageState { get; }

        /// <summary>
        /// 错误信息（如果状态变更是由于错误导致）
        /// </summary>
        public string Error { get; }

        /// <summary>
        /// 创建消息状态变更事件参数
        /// </summary>
        public ChatStateChangedEventArgs(
            string sessionId,
            string messageId,
            ChatMessageState newState,
            ChatMessageState oldState,
            string error = null)
        {
            SessionId = sessionId;
            MessageId = messageId;
            NewMessageState = newState;
            OldMessageState = oldState;
            Error = error;
        }
    }
}
