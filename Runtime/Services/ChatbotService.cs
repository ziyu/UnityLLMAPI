using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityLLMAPI.Config;
using UnityLLMAPI.Interfaces;
using UnityLLMAPI.Models;

namespace UnityLLMAPI.Services
{
    /// <summary>
    /// Service for managing chat conversations and tool interactions
    /// </summary>
    public class ChatbotService : IChatbotService
    {
        private readonly OpenAIService service;
        private ChatSession session;
        private readonly ChatbotConfig config;

        /// <summary>
        /// 状态变更事件
        /// </summary>
        public event EventHandler<ChatStateChangedEventArgs> StateChanged;

        public ChatSession Session => session;
        
        /// <summary>
        /// Get all messages in the conversation history
        /// </summary>
        public IReadOnlyList<ChatMessage> Messages => session.GetAllMessages();

        /// <summary>
        /// 获取当前会话状态
        /// </summary>
        public ChatState CurrentState => session.State;

        public bool IsPending { get; private set; }

        /// <summary>
        /// 是否处于中断状态:session的状态和chatbot状态不一致，判定为被中断
        /// </summary>
        public bool IsInterrupted => !IsPending && session.HasPendingMessages();

        /// <summary>
        /// Initialize a new chatbot service
        /// </summary>
        public ChatbotService(OpenAIService service, ChatbotConfig config)
            : this(service, config, new ChatSession())
        {
        }

        /// <summary>
        /// Initialize a chatbot service with an existing session
        /// </summary>
        public ChatbotService(OpenAIService service, ChatbotConfig config, ChatSession session)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            
            config.ValidateConfig();

            SetSession(session);
        }

        public void SetSession(ChatSession session)
        {
            if (this.session != null)
            {
                ClearPending();
            }

            this.session = session ?? throw new ArgumentNullException(nameof(session));
            // 如果是新会话且有系统提示词，添加系统消息
            if (!this.session.messages.Any() && !string.IsNullOrEmpty(config.systemPrompt))
            {
                AddSystemMessage();
            }
        }

        /// <summary>
        /// Create a System Message
        /// </summary>
        public ChatMessage CreateSystemMessage(string systemPrompt=null)
        {
            if (string.IsNullOrEmpty(systemPrompt))
            {
                systemPrompt = config.systemPrompt;
            }
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                return OpenAIService.CreateSystemMessage(systemPrompt);
            }
            return null;
        }

        public void AddSystemMessage(string systemPrompt=null)
        {
            var systemMessage = CreateSystemMessage(systemPrompt);
            if (systemMessage != null)
            {
                AddMessage(systemMessage);
            }
        }

        /// <summary>
        /// Add a message to the conversation
        /// </summary>
        public void AddMessage(ChatMessage message)
        {
            if (message==null) throw new ArgumentException("Message cannot be empty", nameof(message));
            if (string.IsNullOrEmpty(message.role)) throw new ArgumentException("Role cannot be empty", nameof(ChatMessage.role));
            if (string.IsNullOrEmpty(message.content)) throw new ArgumentException("Content cannot be empty", nameof(ChatMessage.content));
            
            session.AddMessage(message);
        }

        /// <summary>
        /// 获取指定消息的状态
        /// </summary>
        public ChatMessageState GetMessageState(string messageId)
        {
            return session.GetMessageState(messageId);
        }

        /// <summary>
        /// 获取所有pending状态的消息
        /// </summary>
        public IReadOnlyList<ChatMessageInfo> GetAllMessageInfos()
        {
            return session.GetAllMessageInfos();
        }

        /// <summary>
        /// 尝试恢复中断的会话
        /// </summary>
        public async Task<ChatMessage> ResumeSession(ChatParams @params)
        {
            if (!IsInterrupted) return null;

            var pendingMessage = session.GetLastNonToolPendingMessage();
            if (pendingMessage == null) return null;
            return await SendMessage_internal(pendingMessage, @params);
        }

        /// <summary>
        /// Add a user message and get the assistant's response
        /// </summary>
        public async Task<ChatMessage> SendMessage(string message, ChatParams @params)
        {
            if (string.IsNullOrEmpty(message)) throw new ArgumentException("Message cannot be empty", nameof(message));
            if (IsPending) throw new InvalidOperationException("A message is already being sent");

            IsPending = true;
            // Add user message to history
            var userMessage = OpenAIService.CreateUserMessage(message);
            var userMessageInfo = session.AddMessage(userMessage, true);
            UpdateMessageState(userMessageInfo.messageId, ChatMessageState.Sending);

            return await SendMessage_internal(userMessageInfo, @params);
        }

        private async Task<ChatMessage> SendMessage_internal(ChatMessageInfo currentMessageInfo, ChatParams @params)
        {
            IsPending = true;
            try
            {
                // Get response from OpenAI
                ChatMessage response = await GetResponse(currentMessageInfo, @params.Model ?? config.defaultModel,
                    @params.CancellationToken);
                if (response != null)
                {
                    // 完成用户消息
                    UpdateMessageState(currentMessageInfo.messageId, ChatMessageState.Succeeded);
                    session.CompleteAllPendingMessages();
                }
                else
                {
                    UpdateMessageState(currentMessageInfo.messageId, ChatMessageState.Failed);
                    session.CompleteAllPendingMessages(ChatMessageState.Cancelled);
                }
          
                IsPending = false;
                return response;
            }
            catch (Exception e)
            {
                // 如果发生错误，更新用户消息状态
                UpdateMessageState(currentMessageInfo.messageId, ChatMessageState.Failed, e.Message);
                if (e is OperationCanceledException or TaskCanceledException)
                {
                    session.CompleteAllPendingMessages(ChatMessageState.Cancelled);
                }
                else
                {
                    session.CompleteAllPendingMessages(ChatMessageState.Failed);
                }

                throw;
            }
            finally
            {
                IsPending = false;
            }
        }

        private async Task<ChatMessage> GetResponse(ChatMessageInfo currentMessageInfo, string model,CancellationToken cancellationToken)
        {
            ChatMessageInfo nextMessageInfo = null;
            do
            {
                try
                {
                    nextMessageInfo = null;
                    switch (currentMessageInfo.GetState())
                    {
                        case ChatMessageState.Created:
                        case ChatMessageState.Sending:
                        case ChatMessageState.Receiving:
                            // 发送消息
                            UpdateMessageState(currentMessageInfo.messageId, ChatMessageState.Sending);
                            nextMessageInfo=await HandleChatCompletion(currentMessageInfo,model, cancellationToken);
                            UpdateMessageState(currentMessageInfo.messageId, ChatMessageState.Succeeded);
                            break;
                        case ChatMessageState.ProcessingTool:
                            // 执行工具
                            if (currentMessageInfo.message.tool_calls != null)
                            {
                                UpdateMessageState(currentMessageInfo.messageId,ChatMessageState.ProcessingTool);
                                nextMessageInfo=await HandleToolCalls(currentMessageInfo, cancellationToken);
                                //执行完工具后发送执行结果
                                UpdateMessageState(currentMessageInfo.messageId, ChatMessageState.Sending);
                            }
                            else
                            {
                                UpdateMessageState(currentMessageInfo.messageId, ChatMessageState.Failed);
                            }
                            break;
                    }
                    
                }
                catch (Exception)
                {
                    if (currentMessageInfo != null)
                    {
                        UpdateMessageState(currentMessageInfo.messageId, ChatMessageState.Failed);
                    }
                    throw;
                }

                if(nextMessageInfo!=null)
                    currentMessageInfo = nextMessageInfo;
            } while (nextMessageInfo!=null);
            
            return currentMessageInfo.message;
        }

        private async Task<ChatMessageInfo> HandleChatCompletion(ChatMessageInfo pendingMessageInfo, string model,CancellationToken cancellationToken)
        {
            if (pendingMessageInfo == null) return null;
            ChatMessage response;
            // Get response from OpenAI
            if (config.useStreaming)
            {
                response =await service.ChatCompletionStreaming(
                    session.GetAllMessages().ToList(),
                    OnStreamingChunk,
                    model,
                    config.toolSet?.Tools,
                    cancellationToken
                );
            }
            else
            {
                response = await service.ChatCompletion(
                    session.GetAllMessages().ToList(),
                    model,
                    config.toolSet?.Tools,
                    cancellationToken
                );
            }

            if (response == null||pendingMessageInfo.IsCompleted())
            {
                return null;
            }
            var responsMessageInfo = session.AddMessage(response,true);
            var state = ChatMessageState.Succeeded;
            if (response.tool_calls is { Length: > 0 })
            {
                state = ChatMessageState.ProcessingTool;
            }
            UpdateMessageState(responsMessageInfo.messageId,state);
            return responsMessageInfo;
        }

        private async Task<ChatMessageInfo> HandleToolCalls(ChatMessageInfo pendingMessageInfo, CancellationToken cancellationToken)
        {
            if(pendingMessageInfo==null)return null;

            bool CheckIfStateInvalid()
            {
                cancellationToken.ThrowIfCancellationRequested();
                return !pendingMessageInfo.IsCompleted();
            }

            foreach (var toolCall in pendingMessageInfo.message.tool_calls)
            {
                
                if(!CheckIfStateInvalid())break;
                if (session.pendingMessages.Any(x => x.message.tool_call_id == toolCall.id))
                {
                    continue;
                }

                bool shouldExecute = true;
                if (config.shouldExecuteTool != null)
                {
                    shouldExecute = await config.shouldExecuteTool(toolCall);
                    if(!CheckIfStateInvalid())break;
                }

                if (!shouldExecute)
                {
                    var skipResponse = ChatMessage.CreateToolResponse(
                        toolCall.id,
                        toolCall.function.name,
                        config.skipToolMessage
                    );
                    var skipResponseInfo = session.AddMessage(skipResponse,true);
                    UpdateMessageState(skipResponseInfo.messageId, ChatMessageState.Succeeded);
                    continue;
                }

                try
                {
                    string toolResult = await config.toolSet.ExecuteTool(toolCall);
                    if(!CheckIfStateInvalid())break;
                    
                    var toolResponse = ChatMessage.CreateToolResponse(
                        toolCall.id,
                        toolCall.function.name,
                        toolResult
                    );
                    var toolResponseInfo = session.AddMessage(toolResponse,true);
                    UpdateMessageState(toolResponseInfo.messageId, ChatMessageState.Succeeded);
                }
                catch (Exception e)
                {
                    var errorResponse = ChatMessage.CreateToolResponse(
                        toolCall.id,
                        toolCall.function.name,
                        $"Error executing tool: {e.Message}"
                    );
                    var errorResponseInfo = session.AddMessage(errorResponse,true);
                    UpdateMessageState(errorResponseInfo.messageId, ChatMessageState.Failed, e.Message);
                    throw;
                }
            }
            return pendingMessageInfo;
        }

        void OnStreamingChunk(ChatMessage chatMessage)
        {
            config.onStreamingChunk?.Invoke(chatMessage);
        }

        /// <summary>
        /// Clear conversation history
        /// </summary>
        public void ClearHistory(bool keepSystemMessage=true)
        {
            session.ClearHistory(keepSystemMessage);
        }

        /// <summary>
        /// Clear Pending State
        /// </summary>
        public void ClearPending()
        {
            session.CompleteAllPendingMessages(ChatMessageState.Cancelled);
            IsPending = false;
        }

        /// <summary>
        /// 删除指定的消息
        /// </summary>
        /// <param name="messageId">要删除的消息ID</param>
        /// <param name="keepSystemMessage">是否保留系统消息，如果要删除的是系统消息且此参数为true，则不会删除</param>
        /// <param name="clearPending"></param>
        /// <returns>是否成功删除消息</returns>
        public bool DeleteMessage(string messageId, bool keepSystemMessage = true,bool clearPending=true)
        {
            if (string.IsNullOrEmpty(messageId))
                throw new ArgumentException("Message ID cannot be empty", nameof(messageId));

            if (IsPending)
                throw new InvalidOperationException("Cannot delete messages while processing a request");

            if (clearPending)
            {
                session.CompleteAllPendingMessages(ChatMessageState.Cancelled);
            }

            var oldState = GetMessageState(messageId);
            bool deleted = session.DeleteMessage(messageId, keepSystemMessage);

            if (deleted)
            {
                OnStateChanged(new ChatStateChangedEventArgs(
                    session.sessionId,
                    messageId,
                    ChatMessageState.Cancelled,
                    oldState,
                    null
                ));
            }

            return deleted;
        }

        /// <summary>
        /// 更新消息状态并触发事件
        /// </summary>
        private void UpdateMessageState(string messageId, ChatMessageState newState, string error = null)
        {
            var message = session.GetMessageInfo(messageId);
            if (message != null)
            {
                var oldState = message.GetState();
                session.UpdateMessageState(messageId,newState,error);

                OnStateChanged(new ChatStateChangedEventArgs(
                    session.sessionId,
                    messageId,
                    newState,
                    oldState,
                    error
                ));
            }
        }

        /// <summary>
        /// 触发状态变更事件
        /// </summary>
        protected virtual void OnStateChanged(ChatStateChangedEventArgs e)
        {
            StateChanged?.Invoke(this, e);
        }
    }
}
