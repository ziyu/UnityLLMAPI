using UnityEngine;
using System.Collections.Generic;
using UnityLLMAPI.Services;
using UnityLLMAPI.Models;

namespace UnityLLMAPI.Examples
{
    /// <summary>
    /// Example chat implementation using Unity LLM API with IMGUI
    /// </summary>
    public class ChatExample : MonoBehaviour
    {
        [Header("Chat Settings")]
        [Tooltip("System message to set AI behavior")]
        [SerializeField] private string systemMessage = "You are a helpful assistant. Please provide clear and concise responses.";

        private OpenAIService openAIService;
        private List<ChatMessage> chatHistory;
        private bool isProcessing = false;
        private string userInput = "";
        private Vector2 scrollPosition;
        private Rect windowRect = new Rect(10, 10, 400, 600);

        private void Start()
        {
            // Initialize service and chat history
            openAIService = new OpenAIService();
            chatHistory = new List<ChatMessage>();

            // Add system message
            chatHistory.Add(OpenAIService.CreateSystemMessage(systemMessage));
        }

        private void OnGUI()
        {
            windowRect = GUILayout.Window(0, windowRect, DrawChatWindow, "Chat with AI");
        }

        private void DrawChatWindow(int windowID)
        {
            GUILayout.BeginVertical();

            // Chat history area
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(500));
            foreach (var message in chatHistory)
            {
                if (message.role != "system") // Don't display system messages
                {
                    string role = char.ToUpper(message.role[0]) + message.role.Substring(1);
                    GUILayout.Label($"{role}:", EditorStyles.boldLabel);
                    GUILayout.TextArea(message.content, EditorStyles.wordWrappedLabel);
                    GUILayout.Space(10);
                }
            }
            GUILayout.EndScrollView();

            // Input area
            GUI.enabled = !isProcessing;
            GUILayout.BeginHorizontal();
            
            // Input field
            userInput = GUILayout.TextField(userInput, GUILayout.ExpandWidth(true));
            
            // Send button
            if (GUILayout.Button("Send", GUILayout.Width(60)) && !string.IsNullOrEmpty(userInput))
            {
                SendMessage();
            }
            
            GUILayout.EndHorizontal();
            GUI.enabled = true;

            // Processing indicator
            if (isProcessing)
            {
                GUILayout.Label("Processing...");
            }

            GUILayout.EndVertical();

            // Make window draggable
            GUI.DragWindow();
        }

        private async void SendMessage()
        {
            if (isProcessing || string.IsNullOrEmpty(userInput))
                return;

            try
            {
                isProcessing = true;
                string userMessage = userInput;
                userInput = ""; // Clear input field

                // Add user message to history
                chatHistory.Add(OpenAIService.CreateUserMessage(userMessage));

                // Get AI response
                string response = await openAIService.ChatCompletion(chatHistory);

                // Add AI response to history
                chatHistory.Add(OpenAIService.CreateAssistantMessage(response));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in chat: {e.Message}");
                chatHistory.Add(OpenAIService.CreateAssistantMessage("Error: Failed to get response from AI."));
            }
            finally
            {
                isProcessing = false;
            }
        }
    }
}
