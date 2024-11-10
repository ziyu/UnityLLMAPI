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
                        tcs.SetResult(request.downloadHandler.text);
                    }
                    else
                    {
                        tcs.SetException(new Exception($"HTTP Error: {request.error}\nResponse: {request.downloadHandler?.text}"));
                    }
                };

                return await tcs.Task;
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
                        tcs.SetResult(true);
                    }
                    else
                    {
                        tcs.SetException(new Exception($"HTTP Error: {request.error}"));
                    }
                };

                await tcs.Task;
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
                request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                
                TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();
                
                request.SendWebRequest().completed += operation =>
                {
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        tcs.SetResult(request.downloadHandler.text);
                    }
                    else
                    {
                        tcs.SetException(new Exception($"HTTP Error: {request.error}\nResponse: {request.downloadHandler?.text}"));
                    }
                };

                return await tcs.Task;
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
                onData?.Invoke(line);
            }

            return true;
        }
    }
}
