using UnityEngine;

namespace UnityLLMAPI.Config
{
    /// <summary>
    /// Configuration for OpenAI API
    /// </summary>
    [CreateAssetMenu(fileName = "OpenAIConfig", menuName = "UnityLLMAPI/OpenAI Configuration")]
    public class OpenAIConfig : ScriptableObject
    {
        [Header("API Configuration")]
        [Tooltip("Your OpenAI API Key")]
        public string apiKey;

        [Tooltip("API Base URL")]
        public string apiBaseUrl = "https://api.openai.com/v1";

        [Header("Default Settings")]
        [Tooltip("Default model to use")]
        public string defaultModel = "gpt-3.5-turbo";

        [Range(0f, 2f)]
        [Tooltip("Temperature for response randomness")]
        public float temperature = 0.7f;

        [Range(1, 4000)]
        [Tooltip("Maximum tokens in response")]
        public int maxTokens = 1000;

        private static OpenAIConfig instance;
        public static OpenAIConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<OpenAIConfig>("OpenAIConfig");
                    if (instance == null)
                    {
                        Debug.LogError("OpenAIConfig not found in Resources folder!");
                    }
                }
                return instance;
            }
        }
    }
}
