using UnityEngine;

namespace UnityLLMAPI.Utils
{
    public static class LLMLogging
    {
        private static bool isLoggingEnabled = true;
        private static int logLevel =0;
        private const string LoggingTag = "[LLM]";

        public static void EnableLogging(bool enable)
        {
            isLoggingEnabled = enable;
        }

        public static void SetLogLevel(LogType logType)
        {
            logLevel = LogTypeToLogLevel(logType);
        }

        static int LogTypeToLogLevel(LogType logType)
        {
            if (logType == LogType.Log)
            {
                return -1;
            }
            else
            {
                return (int)logType;
            }
        }

        public static void Log(string message, LogType logType = LogType.Log)
        {
            if (!isLoggingEnabled) return;
            
            if(LogTypeToLogLevel(logType)<logLevel)return;

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
