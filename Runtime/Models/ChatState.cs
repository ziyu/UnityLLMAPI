using System;

namespace UnityLLMAPI.Models
{
    /// <summary>
    /// 表示聊天消息的发送状态
    /// </summary>
    [Serializable]
    public enum ChatState
    {
        /// <summary>
        /// 初始状态
        /// </summary>
        Ready,

        /// <summary>
        /// 发送中、消息处理中等，具体状态取决于消息状态
        /// </summary>
        Pending,
    }
}
