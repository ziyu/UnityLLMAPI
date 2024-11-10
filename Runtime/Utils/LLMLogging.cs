using UnityEngine;

namespace UnityLLMAPI.Utils
{
    public static class LLMLogging
    {
        private static bool isLoggingEnabled = true;
        private const string LoggingTag = "[LLM]";

        public static void EnableLogging(bool enable)
        {
            isLoggingEnabled = enable;
        }

        public static void Log(string message, LogType logType = LogType.Log)
        {
            if (!isLoggingEnabled) return;

            switch (logType)
            {
                case LogType.Error:
                case LogType.Exception:
                    Debug.LogError($"{LoggingTag} {message}");
                    break;
                case LogType.Warning:
                    Debug.LogWarning($"{LoggingTag} {message}");
                    break;
                case LogType.Log:
                    Debug.Log($"{LoggingTag}{message}");
                    break;
                case LogType.Assert:
                    Debug.LogAssertion($"{LoggingTag} {message}");
                    break;
            }
        }
    }
}
