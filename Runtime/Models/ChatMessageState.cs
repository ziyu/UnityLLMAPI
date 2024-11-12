using System;

namespace UnityLLMAPI.Models
{
    /// <summary>
    /// 表示单个聊天消息的状态
    /// </summary>
    [Serializable]
    public enum ChatMessageState
    {
        /// <summary>
        /// 消息刚创建，等待处理
        /// </summary>
        Created,
        
        /// <summary>
        /// 消息正在发送中
        /// </summary>
        Sending,

        /// <summary>
        /// 消息正在接收流式响应
        /// </summary>
        Receiving,

        /// <summary>
        /// 消息涉及工具调用正在执行
        /// </summary>
        ProcessingTool,

        /// <summary>
        /// 消息处理成功完成
        /// </summary>
        Succeeded,

        /// <summary>
        /// 消息处理失败
        /// </summary>
        Failed,

        /// <summary>
        /// 消息被用户取消
        /// </summary>
        Cancelled
    }
}
