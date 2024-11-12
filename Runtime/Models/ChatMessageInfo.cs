using System;

namespace UnityLLMAPI.Models
{
    /// <summary>
    /// 聊天消息的完整信息，包含消息内容、状态和元数据
    /// </summary>
    [Serializable]
    public class ChatMessageInfo
    {
        /// <summary>
        /// 消息唯一标识符
        /// </summary>
        public string messageId;

        /// <summary>
        /// 消息内容
        /// </summary>
        public ChatMessage message;

        /// <summary>
        /// 消息创建时间戳（Unix时间戳）
        /// </summary>
        public long timestamp;

        /// <summary>
        /// 消息状态
        /// </summary>
        public string state;

        /// <summary>
        /// 如果消息发送失败，这里存储错误信息
        /// </summary>
        public string errorMessage;

        /// <summary>
        /// 消息的token数量（用于跟踪token使用情况）
        /// </summary>
        public int tokenCount;

        public ChatMessageInfo()
        {
            messageId = Guid.NewGuid().ToString();
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            state = ChatMessageState.Created.ToString();
        }

        public ChatMessageInfo(ChatMessage message) : this()
        {
            this.message = message ?? throw new ArgumentNullException(nameof(message));
        }

        /// <summary>
        /// 更新消息状态
        /// </summary>
        public void UpdateState(ChatMessageState newState, string error = null)
        {
            state = newState.ToString();
            if (newState == ChatMessageState.Failed && !string.IsNullOrEmpty(error))
            {
                errorMessage = error;
            }
        }

        /// <summary>
        /// 获取消息状态
        /// </summary>
        public ChatMessageState GetState()
        {
            return (ChatMessageState)Enum.Parse(typeof(ChatMessageState), state);
        }

        /// <summary>
        /// 检查消息是否处于特定状态
        /// </summary>
        public bool IsInState(ChatMessageState checkState)
        {
            return GetState() == checkState;
        }

        /// <summary>
        /// 检查消息是否已完成（成功或失败）
        /// </summary>
        public bool IsCompleted()
        {
            var currentState = GetState();
            return currentState == ChatMessageState.Succeeded || 
                   currentState == ChatMessageState.Failed || 
                   currentState == ChatMessageState.Cancelled;
        }
    }
}
