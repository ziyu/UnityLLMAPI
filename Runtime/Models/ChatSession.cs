using System;
using System.Collections.Generic;
using System.Linq;
using UnityLLMAPI.Utils.Json;

namespace UnityLLMAPI.Models
{
    /// <summary>
    /// 表示一个完整的聊天会话
    /// </summary>
    [Serializable]
    public class ChatSession
    {
        /// <summary>
        /// 会话唯一标识符
        /// </summary>
        public string sessionId;

        /// <summary>
        /// 会话创建时间（Unix时间戳）
        /// </summary>
        public long createdAt;

        /// <summary>
        /// 会话最后更新时间（Unix时间戳）
        /// </summary>
        public long updatedAt;

        /// <summary>
        /// 会话标题
        /// </summary>
        public string title;

        /// <summary>
        /// 会话描述
        /// </summary>
        public string description;

        /// <summary>
        /// 已完成的消息列表
        /// </summary>
        public List<ChatMessageInfo> messages;

        /// <summary>
        /// 正在处理中的消息列表
        /// </summary>
        public List<ChatMessageInfo> pendingMessages;

        /// <summary>
        /// 会话的当前状态
        /// </summary>
        public string state;

        /// <summary>
        /// 会话的自定义元数据
        /// </summary>
        public Dictionary<string, string> metadata;

        // 缓存的消息列表
        private List<ChatMessage> cachedCompletedMessages;
        private List<ChatMessage> cachedAllMessages;
        private List<ChatMessageInfo> cachedAllMessageInfos;
        private bool isDirty = true;

        /// <summary>
        /// 获取当前会话状态
        /// </summary>
        public ChatState State
        {
            get
            {
                if (Enum.TryParse<ChatState>(state, out var result))
                {
                    return result;
                }
                state = ChatState.Ready.ToString();
                return ChatState.Ready;
            }
        }

        public ChatSession()
        {
            sessionId = Guid.NewGuid().ToString();
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            createdAt = now;
            updatedAt = now;
            messages = new List<ChatMessageInfo>();
            pendingMessages = new List<ChatMessageInfo>();
            metadata = new Dictionary<string, string>();
            state = ChatState.Ready.ToString();
            cachedCompletedMessages = new List<ChatMessage>();
            cachedAllMessages = new List<ChatMessage>();
            cachedAllMessageInfos = new();
        }

        /// <summary>
        /// 添加消息到会话
        /// </summary>
        internal ChatMessageInfo AddMessage(ChatMessage message, bool isPending = false)
        {
            if (message == null)
                throw new ArgumentNullException(nameof(message));

            var messageInfo = new ChatMessageInfo(message);
            
            if (isPending)
            {
                pendingMessages.Add(messageInfo);
                messageInfo.UpdateState(ChatMessageState.Created);
                UpdateState(ChatState.Pending);
            }
            else
            {
                messages.Add(messageInfo);
                messageInfo.UpdateState(ChatMessageState.Succeeded);
            }

            isDirty = true;
            updatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            return messageInfo;
        }

        /// <summary>
        /// 将pending消息移动到已完成消息列表
        /// </summary>
        /// <param name="state"></param>
        internal bool CompleteAllPendingMessages(ChatMessageState state=ChatMessageState.Succeeded)
        {
            if (!ChatMessageInfo.IsCompleteState(state))
            {
                throw new Exception("Must pass completed state.");
            }

            if (pendingMessages.Count == 0) return false;
            foreach (var pendingMessage in pendingMessages)
            {
                messages.Add(pendingMessage);
                if(!pendingMessage.IsCompleted())
                    pendingMessage.UpdateState(state);
            }
            pendingMessages.Clear();
            UpdateState(ChatState.Ready,true);
            return true;
        }
        
        // internal bool CancelAllPendingMessages()
        // {
        //     if (pendingMessages.Count == 0) return false;
        //     foreach (var pendingMessage in pendingMessages)
        //     {
        //         if(!pendingMessage.IsCompleted())
        //             pendingMessage.UpdateState(ChatMessageState.Cancelled);
        //     }
        //     pendingMessages.Clear();
        //     UpdateState(ChatState.Ready,true);
        //     return true;
        // }

        /// <summary>
        /// 更新消息状态
        /// </summary>
        internal void UpdateMessageState(string messageId, ChatMessageState newState, string error = null)
        {
            var message = GetMessageInfo(messageId);
            UpdateMessageState(message,newState,error);
        }
        
        private void UpdateMessageState(ChatMessageInfo message, ChatMessageState newState, string error = null)
        {
            if (message == null)
            {
                return;
            }
            message.UpdateState(newState, error);
            updatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>
        /// 获取存储的消息
        /// </summary>
        public ChatMessageInfo GetMessageInfo(string messageId)
        {
            return messages.FirstOrDefault(m => m.messageId == messageId) ?? 
                   pendingMessages.FirstOrDefault(m => m.messageId == messageId);
        }

        /// <summary>
        /// 获取所有消息（包括pending消息）
        /// </summary>
        public IReadOnlyList<ChatMessage> GetAllMessages(bool includePending = true)
        {
            if (isDirty)
            {
                UpdateCachedMessages();
            }
            return includePending ? cachedAllMessages : cachedCompletedMessages;
        }
        
        /// <summary>
        /// 获取所有存储的消息（包括pending消息）
        /// </summary>
        public IReadOnlyList<ChatMessageInfo> GetAllMessageInfos()
        {
            if (isDirty)
            {
                UpdateCachedMessages();
            }
            return cachedAllMessageInfos;
        }
        
        private void UpdateCachedMessages()
        {
            // 更新已完成消息缓存
            cachedCompletedMessages.Clear();
            cachedCompletedMessages.AddRange(messages.Select(m => m.message));

            // 更新所有消息缓存
            cachedAllMessages.Clear();
            cachedAllMessages.AddRange(cachedCompletedMessages);
            cachedAllMessages.AddRange(pendingMessages.Select(m => m.message));
            
            cachedAllMessageInfos.Clear();
            cachedAllMessageInfos.AddRange(messages);
            cachedAllMessageInfos.AddRange(pendingMessages);

            isDirty = false;
        }

        /// <summary>
        /// 清除会话历史记录
        /// </summary>
        public void ClearHistory(bool keepSystemMessage = true, bool clearPending = true)
        {
            if (keepSystemMessage && messages.Count > 0 && messages[0].message.role == "system")
            {
                var systemMessage = messages[0];
                messages.Clear();
                messages.Add(systemMessage);
            }
            else
            {
                messages.Clear();
            }

            if (clearPending)
            {
                foreach (var pendingMessage in pendingMessages)
                {
                    pendingMessage.UpdateState(ChatMessageState.Cancelled);
                }
                pendingMessages.Clear();
            }
            
            UpdateState(ChatState.Ready,true);
        }

        /// <summary>
        /// 删除指定的消息
        /// </summary>
        /// <param name="messageId">要删除的消息ID</param>
        /// <param name="keepSystemMessage">是否保留系统消息，如果要删除的是系统消息且此参数为true，则不会删除</param>
        /// <returns>是否成功删除消息</returns>
        internal bool DeleteMessage(string messageId, bool keepSystemMessage = true)
        {
            if (State == ChatState.Pending) return false;
            
            // 获取要删除的消息
            var messageToDelete = GetMessageInfo(messageId);
            if (messageToDelete == null)
            {
                return false;
            }

            // 如果是系统消息且需要保留，则不删除
            if (keepSystemMessage && messageToDelete.message.role == "system" && 
                messages.Count > 0 && messages[0].messageId == messageId)
            {
                return false;
            }

            // 如果消息包含tool calls，需要同时删除对应的tool result消息
            if (messageToDelete.message.tool_calls is { Length: > 0 })
            {
                var toolCallIds = messageToDelete.message.tool_calls.Select(tc => tc.id).ToList();
                messages.RemoveAll(m => m.message.tool_call_id != null && toolCallIds.Contains(m.message.tool_call_id));
            }

            // 从消息列表中删除
            bool removed = messages.Remove(messageToDelete);
            if (!removed)
            {
                pendingMessages.Remove(messageToDelete);
            }

            if (removed)
            {
                isDirty = true;
                updatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }

            return removed;
        }

        private void UpdateState(ChatState newState,bool dirty=false)
        {
            isDirty = dirty;
            state = newState.ToString();
            updatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        }

        /// <summary>
        /// 获取会话的序列化数据
        /// </summary>
        public string ToJson()
        {
            return JsonConverter.SerializeObject(this);
        }

        /// <summary>
        /// 从序列化数据恢复会话
        /// </summary>
        public static ChatSession FromJson(string json)
        {
            return JsonConverter.DeserializeObject<ChatSession>(json);
        }

        /// <summary>
        /// 检查是否有未完成的消息
        /// </summary>
        public bool HasPendingMessages()
        {
            return pendingMessages.Any();
        }

        /// <summary>
        /// 获取最后一条不是工具调用的pending消息
        /// </summary>
        public ChatMessageInfo GetLastNonToolPendingMessage()
        {
            return pendingMessages.LastOrDefault(x=>x.message.role!="tool");
        }

        /// <summary>
        /// 获取消息的状态
        /// </summary>
        public ChatMessageState GetMessageState(string messageId)
        {
            var message = GetMessageInfo(messageId);
            return message?.GetState() ?? ChatMessageState.Created;
        }

        /// <summary>
        /// 检查消息是否处于特定状态
        /// </summary>
        public bool IsMessageInState(string messageId, ChatMessageState checkState)
        {
            var message = GetMessageInfo(messageId);
            return message?.IsInState(checkState) ?? false;
        }
    }
}
