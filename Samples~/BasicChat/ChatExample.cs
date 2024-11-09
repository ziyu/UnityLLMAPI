using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityLLMAPI.Services;
using UnityLLMAPI.Models;
using TMPro;

namespace UnityLLMAPI.Examples
{
    /// <summary>
    /// Example MonoBehaviour showing how to use the OpenAI API plugin
    /// </summary>
    public class ChatExample : MonoBehaviour
    {
        [SerializeField]
        private TMP_InputField userInputField;
        
        [SerializeField]
        private TMP_Text responseText;
        
        [SerializeField]
        private Button sendButton;

        private OpenAIService openAIService;
        private List<ChatMessage> chatHistory;

        private void Start()
        {
            // Initialize the service
            openAIService = new OpenAIService();
            chatHistory = new List<ChatMessage>();

            // Add a system message to set the context
            chatHistory.Add(OpenAIService.CreateSystemMessage(
                "You are a helpful assistant. Please provide clear and concise responses."
            ));

            // Setup UI
            if (sendButton != null)
            {
                sendButton.onClick.AddListener(SendMessage);
            }
        }

        public async void SendMessage()
        {
            if (string.IsNullOrEmpty(userInputField.text))
                return;

            try
            {
                // Disable input while processing
                SetInteractable(false);
                
                // Add user message to history
                string userMessage = userInputField.text;
                chatHistory.Add(OpenAIService.CreateUserMessage(userMessage));
                
                // Clear input field
                userInputField.text = "";
                
                // Update display
                DisplayChatHistory();
                
                // Get response from API
                string response = await openAIService.ChatCompletion(chatHistory);
                
                // Add response to history
                chatHistory.Add(OpenAIService.CreateAssistantMessage(response));
                
                // Update display
                DisplayChatHistory();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error sending message: {e.Message}");
                responseText.text = "Error: Could not get response from API";
            }
            finally
            {
                // Re-enable input
                SetInteractable(true);
            }
        }

        private void DisplayChatHistory()
        {
            string display = "";
            foreach (var message in chatHistory)
            {
                if (message.role != "system") // Don't display system messages
                {
                    string role = char.ToUpper(message.role[0]) + message.role.Substring(1);
                    display += $"{role}: {message.content}\n\n";
                }
            }
            responseText.text = display;
        }

        private void SetInteractable(bool interactable)
        {
            if (userInputField != null)
                userInputField.interactable = interactable;
            if (sendButton != null)
                sendButton.interactable = interactable;
        }
    }
}
