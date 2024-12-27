using UnityEngine;
using UnityLLMAPI.Utils;

namespace UnityLLMAPI.Config
{
    [CreateAssetMenu(fileName = "OpenAIConfig", menuName = "LLM API/OpenAI Config")]
    public class OpenAIConfig : ScriptableObject
    {
        private static OpenAIConfig instance;
        public static OpenAIConfig Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<OpenAIConfig>("OpenAIConfig");
                }
                return instance;
            }
        }

        [Header("API Configuration")]
        public string apiKey;
        public string apiBaseUrl = "https://api.openai.com/v1";
        public string defaultModel = "gpt-4o-mini";
        
        [Header("Model Configuration")]
        [Range(0f, 2f)]
        public float temperature = 0.7f;
        public int maxTokens = 2000;

        [Header("Debug Configuration")]
        public bool enableLogging = true;
        public LogType minimumLogLevel = LogType.Error;

        public void ValidateConfig()
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw new LLMConfigurationException("API key is not configured");
            }

            if (string.IsNullOrEmpty(apiBaseUrl))
            {
                throw new LLMConfigurationException("API base URL is not configured");
            }

            if (string.IsNullOrEmpty(defaultModel))
            {
                throw new LLMConfigurationException("Default model is not configured");
            }
        }
    }
}
