using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace UnityLLMAPI.Utils
{
    /// <summary>
    /// Utility class for handling HTTP requests
    /// </summary>
    public class HttpClient
    {
        /// <summary>
        /// Send a POST request with JSON data
        /// </summary>
        /// <param name="url">Target URL</param>
        /// <param name="jsonData">JSON data to send</param>
        /// <param name="apiKey">API key for authorization</param>
        /// <returns>Response as string</returns>
        public static async Task<string> PostJsonAsync(string url, string jsonData, string apiKey)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                try
                {
                    LLMLogging.Log($"Sending POST request to {url}", LogType.Log);
                    
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new DownloadHandlerBuffer();
                    
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                    TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
                    
                    request.SendWebRequest().completed += operation =>
                    {
                        if (request.result == UnityWebRequest.Result.Success)
                        {
                            LLMLogging.Log("Request completed successfully", LogType.Log);
                            tcs.SetResult(request.downloadHandler.text);
                        }
                        else
                        {
                            string errorMessage = $"HTTP Error: {request.error}";
                            string responseContent = request.downloadHandler?.text;
                            LLMLogging.Log(errorMessage, LogType.Error);
                            
                            if (!string.IsNullOrEmpty(responseContent))
                            {
                                LLMLogging.Log($"Response content: {responseContent}", LogType.Error);
                            }

                            tcs.SetException(new LLMNetworkException(errorMessage+":"+responseContent ));
                        }
                    };

                    return await tcs.Task;
                }
                catch (Exception e)
                {
                    string errorMessage = $"Exception in PostJsonAsync: {e.Message}";
                    LLMLogging.Log(errorMessage, LogType.Error);
                    throw new LLMNetworkException(errorMessage, e);
                }
            }
        }

        /// <summary>
        /// Send a streaming POST request with JSON data
        /// </summary>
        /// <param name="url">Target URL</param>
        /// <param name="jsonData">JSON data to send</param>
        /// <param name="apiKey">API key for authorization</param>
        /// <param name="onData">Callback for receiving raw data lines</param>
        public static async Task PostJsonStreamAsync(string url, string jsonData, string apiKey, Action<string> onData)
        {
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                try
                {
                    LLMLogging.Log($"Sending streaming POST request to {url}", LogType.Log);
                    
                    byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                    request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                    request.downloadHandler = new StreamingDownloadHandler(onData);
                    
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                    request.SetRequestHeader("Accept", "text/event-stream");

                    TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
                    
                    request.SendWebRequest().completed += operation =>
                    {
                        if (request.result == UnityWebRequest.Result.Success)
                        {
                            LLMLogging.Log("Streaming request completed successfully", LogType.Log);
                            tcs.SetResult(true);
                        }
                        else
                        {
                            string errorMessage = $"HTTP Error: {request.error}";
                            LLMLogging.Log(errorMessage, LogType.Error);
                            tcs.SetException(new LLMNetworkException(errorMessage));
                        }
                    };

                    await tcs.Task;
                }
                catch (Exception e)
                {
                    string errorMessage = $"Exception in PostJsonStreamAsync: {e.Message}";
                    LLMLogging.Log(errorMessage, LogType.Error);
                    throw new LLMNetworkException(errorMessage, e);
                }
            }
        }

        /// <summary>
        /// Send a GET request
        /// </summary>
        /// <param name="url">Target URL</param>
        /// <param name="apiKey">API key for authorization</param>
        /// <returns>Response as string</returns>
        public static async Task<string> GetAsync(string url, string apiKey)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                try
                {
                    LLMLogging.Log($"Sending GET request to {url}", LogType.Log);
                    
                    request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                    
                    TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
                    
                    request.SendWebRequest().completed += operation =>
                    {
                        if (request.result == UnityWebRequest.Result.Success)
                        {
                            LLMLogging.Log("GET request completed successfully", LogType.Log);
                            tcs.SetResult(request.downloadHandler.text);
                        }
                        else
                        {
                            string errorMessage = $"HTTP Error: {request.error}";
                            string responseContent = request.downloadHandler?.text;
                            LLMLogging.Log(errorMessage, LogType.Error);
                            
                            if (!string.IsNullOrEmpty(responseContent))
                            {
                                LLMLogging.Log($"Response content: {responseContent}", LogType.Error);
                            }

                            tcs.SetException(new LLMNetworkException(errorMessage+":"+responseContent));
                        }
                    };

                    return await tcs.Task;
                }
                catch (Exception e)
                {
                    string errorMessage = $"Exception in GetAsync: {e.Message}";
                    LLMLogging.Log(errorMessage, LogType.Error);
                    throw new LLMNetworkException(errorMessage, e);
                }
            }
        }
    }

    /// <summary>
    /// Custom download handler for streaming responses
    /// </summary>
    public class StreamingDownloadHandler : DownloadHandlerScript
    {
        private readonly Action<string> onData;
        private StringBuilder buffer = new StringBuilder();

        public StreamingDownloadHandler(Action<string> onData) : base()
        {
            this.onData = onData;
        }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            try
            {
                if (data == null || dataLength == 0) return false;

                string chunk = Encoding.UTF8.GetString(data, 0, dataLength);
                buffer.Append(chunk);

                // Process complete lines
                int newlineIndex;
                while ((newlineIndex = buffer.ToString().IndexOf("\n")) != -1)
                {
                    string line = buffer.ToString(0, newlineIndex).Trim();
                    buffer.Remove(0, newlineIndex + 1);
                    
                    // Pass raw line to callback
                    if (!string.IsNullOrEmpty(line))
                    {
                        LLMLogging.Log($"Received streaming data: {line}", LogType.Log);
                        onData?.Invoke(line);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                LLMLogging.Log($"Error in ReceiveData: {e.Message}", LogType.Error);
                return false;
            }
        }
    }
}
