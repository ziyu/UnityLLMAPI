using System;
using System.Collections.Generic;
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
        private readonly List<ChatMessage> messageHistory = new();
        private readonly ChatbotConfig config;

        /// <summary>
        /// Get all messages in the conversation history
        /// </summary>
        public IReadOnlyList<ChatMessage> Messages => messageHistory;

        /// <summary>
        /// Initialize a new chatbot service
        /// </summary>
        /// <param name="service">OpenAI service instance</param>
        /// <param name="config">Chatbot configuration</param>
        public ChatbotService(OpenAIService service, ChatbotConfig config)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            this.config = config ?? throw new ArgumentNullException(nameof(config));
            
            config.ValidateConfig();

            if (!string.IsNullOrEmpty(config.systemPrompt))
            {
                AddMessage(OpenAIService.CreateSystemMessage(config.systemPrompt));
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
            messageHistory.Add(message);
        }

        /// <summary>
        /// Add a user message and get the assistant's response
        /// </summary>
        /// <param name="message">User message content</param>
        /// <param name="model">Optional model override</param>
        /// <returns>Assistant's response message</returns>
        public async Task<ChatMessage> SendMessage(string message, string model = null)
        {
            if (string.IsNullOrEmpty(message)) throw new ArgumentException("Message cannot be empty", nameof(message));

            // Add user message to history
            var userMessage = OpenAIService.CreateUserMessage(message);
            messageHistory.Add(userMessage);

            // Get response from OpenAI
            ChatMessage response;
            if (config.toolSet != null)
            {
                response = await GetResponseWithTools(model ?? config.defaultModel);
            }
            else if (config.useStreaming)
            {
                await service.ChatCompletionStreaming(messageHistory, OnStreamingChunk, model ?? config.defaultModel);
                // Get the last message from history since streaming updates it
                response = messageHistory[^1];
            }
            else
            {
                response = await service.ChatCompletion(messageHistory, model ?? config.defaultModel);
                messageHistory.Add(response);
            }

            return response;
        }

        private async Task<ChatMessage> GetResponseWithTools(string model)
        {
            ChatMessage response = null;
            bool continueConversation;

            do
            {
                // Get response from OpenAI
                if (config.useStreaming)
                {
                    await service.ChatCompletionStreamingWithTools(
                        messageHistory,
                        config.toolSet.Tools,
                        OnStreamingChunk,
                        model
                    );
                    // Get the last message from history since streaming updates it
                    response = messageHistory[^1];
                }
                else
                {
                    response = await service.ChatCompletionWithTools(
                        messageHistory,
                        config.toolSet.Tools,
                        model
                    );
                    messageHistory.Add(response);
                }

                // Check if we need to handle tool calls
                continueConversation = false;
                if (response.tool_calls is { Length: > 0 })
                {
                    foreach (var toolCall in response.tool_calls)
                    {
                        // Check if we should execute this tool call
                        bool shouldExecute = true;
                        if (config.shouldExecuteTool != null)
                        {
                            shouldExecute = await config.shouldExecuteTool(toolCall);
                        }

                        if (!shouldExecute)
                        {
                            var skipResponse = ChatMessage.CreateToolResponse(
                                toolCall.id,
                                toolCall.function.name,
                                config.skipToolMessage
                            );
                            messageHistory.Add(skipResponse);
                            continue;
                        }

                        try
                        {
                            string toolResult = await config.toolSet.ExecuteTool(toolCall);
                            var toolResponse = ChatMessage.CreateToolResponse(
                                toolCall.id,
                                toolCall.function.name,
                                toolResult
                            );
                            messageHistory.Add(toolResponse);
                            continueConversation = true;
                        }
                        catch (Exception e)
                        {
                            // Add error as tool response and stop conversation
                            var errorResponse = ChatMessage.CreateToolResponse(
                                toolCall.id,
                                toolCall.function.name,
                                $"Error executing tool: {e.Message}"
                            );
                            messageHistory.Add(errorResponse);
                            continueConversation = false;
                            break;
                        }
                    }
                }
            } while (continueConversation);

            return response;
        }

        void OnStreamingChunk(ChatMessage chatMessage,bool isDone)
        {
            if (isDone)
            {
                messageHistory.Add(chatMessage);
            }
            config.onStreamingChunk?.Invoke(chatMessage,isDone);
        }


        /// <summary>
        /// Clear conversation history
        /// </summary>
        public void ClearHistory()
        {
            messageHistory.Clear();
        }
    }
}
