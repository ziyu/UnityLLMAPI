using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityLLMAPI.Services;
using UnityLLMAPI.Models;

namespace UnityLLMAPI.Examples
{
    /// <summary>
    /// Example chat implementation using Unity LLM API
    /// </summary>
    public class ChatExample : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Input field for user messages")]
        [SerializeField] private TMP_InputField userInputField;
        
        [Tooltip("Text area for displaying chat history")]
        [SerializeField] private TMP_Text chatHistoryText;
        
        [Tooltip("Button to send messages")]
        [SerializeField] private Button sendButton;

        [Header("Chat Settings")]
        [Tooltip("System message to set AI behavior")]
        [SerializeField] private string systemMessage = "You are a helpful assistant. Please provide clear and concise responses.";

        private OpenAIService openAIService;
        private List<ChatMessage> chatHistory;
        private bool isProcessing = false;

        private void Start()
        {
            // Initialize service and chat history
            openAIService = new OpenAIService();
            chatHistory = new List<ChatMessage>();

            // Add system message
            chatHistory.Add(OpenAIService.CreateSystemMessage(systemMessage));

            // Setup UI event listeners
            if (sendButton != null)
                sendButton.onClick.AddListener(SendMessage);

            if (userInputField != null)
            {
                userInputField.onSubmit.AddListener(_ => SendMessage());
            }

            // Initial UI state
            UpdateUIState();
        }

        public async void SendMessage()
        {
            if (isProcessing || string.IsNullOrEmpty(userInputField?.text))
                return;

            try
            {
                isProcessing = true;
                UpdateUIState();

                // Get user input and clear input field
                string userMessage = userInputField.text;
                userInputField.text = "";

                // Add user message to history
                chatHistory.Add(OpenAIService.CreateUserMessage(userMessage));
                UpdateChatDisplay();

                // Get AI response
                string response = await openAIService.ChatCompletion(chatHistory);

                // Add AI response to history
                chatHistory.Add(OpenAIService.CreateAssistantMessage(response));
                UpdateChatDisplay();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in chat: {e.Message}");
                chatHistoryText.text += "\n<color=red>Error: Failed to get response from AI.</color>\n";
            }
            finally
            {
                isProcessing = false;
                UpdateUIState();
            }
        }

        private void UpdateChatDisplay()
        {
            if (chatHistoryText == null) return;

            string display = "";
            foreach (var message in chatHistory)
            {
                if (message.role != "system") // Don't display system messages
                {
                    string role = char.ToUpper(message.role[0]) + message.role.Substring(1);
                    display += $"<b>{role}:</b> {message.content}\n\n";
                }
            }
            chatHistoryText.text = display;
        }

        private void UpdateUIState()
        {
            if (sendButton != null)
                sendButton.interactable = !isProcessing;
            if (userInputField != null)
            {
                userInputField.interactable = !isProcessing;
                userInputField.placeholder.GetComponent<TMP_Text>().text = 
                    isProcessing ? "Waiting for response..." : "Type your message...";
            }
        }

        private void OnValidate()
        {
            // Validate required components
            if (userInputField == null)
                Debug.LogWarning("ChatExample: User Input Field reference is missing");
            if (chatHistoryText == null)
                Debug.LogWarning("ChatExample: Chat History Text reference is missing");
            if (sendButton == null)
                Debug.LogWarning("ChatExample: Send Button reference is missing");
        }
    }
}
