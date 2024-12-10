using System;
using System.Text;
using System.Threading;
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
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Response as string</returns>
        public static async Task<string> PostJsonAsync(string url, string jsonData, string apiKey, CancellationToken cancellationToken = default)
        {
            UnityWebRequest request = null;
            try
            {
                request = new UnityWebRequest(url, "POST");
                LLMLogging.Log($"Sending POST request to {url}", LogType.Log);
                
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {apiKey}");

                TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

                // 注册取消回调
                cancellationToken.Register(() => 
                {
                    if (request is { isDone: false })
                    {
                        LLMLogging.Log("Request cancelled by user", LogType.Log);
                        request.Abort();
                        tcs.TrySetCanceled();
                    }
                });
                
                request.SendWebRequest().completed += operation =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        LLMLogging.Log("Request completed successfully", LogType.Log);
                        tcs.TrySetResult(request.downloadHandler.text);
                    }
                    else if (request.result != UnityWebRequest.Result.ConnectionError || !cancellationToken.IsCancellationRequested)
                    {
                        string errorMessage = $"HTTP Error: {request.error}";
                        string responseContent = request.downloadHandler?.text;
                        LLMLogging.Log(errorMessage, LogType.Error);
                        
                        if (!string.IsNullOrEmpty(responseContent))
                        {
                            LLMLogging.Log($"Response content: {responseContent}", LogType.Error);
                        }

                        tcs.TrySetException(new LLMNetworkException(errorMessage+":"+responseContent));
                    }
                };

                return await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                LLMLogging.Log("Operation was cancelled", LogType.Log);
                throw;
            }
            catch (Exception e)
            {
                string errorMessage = $"Exception in PostJsonAsync: {e.Message}";
                LLMLogging.Log(errorMessage, LogType.Error);
                throw new LLMNetworkException(errorMessage, e);
            }
            finally
            {
                if (request != null)
                {
                    request.Dispose();
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
        /// <param name="cancellationToken">Token to cancel the operation</param>
        public static async Task PostJsonStreamAsync(string url, string jsonData, string apiKey, Action<string> onData, CancellationToken cancellationToken = default)
        {
            UnityWebRequest request = null;
            try
            {
                request = new UnityWebRequest(url, "POST");
                LLMLogging.Log($"Sending streaming POST request to {url}", LogType.Log);
                
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new StreamingDownloadHandler(onData);
                
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                request.SetRequestHeader("Accept", "text/event-stream");
                request.SetRequestHeader("Cache-Control", "no-cache");

                TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

                // 注册取消回调
                cancellationToken.Register(() => 
                {
                    if (request is { isDone: false })
                    {
                        LLMLogging.Log("Request cancelled by user", LogType.Log);
                        request.Abort();
                        tcs.TrySetCanceled();
                    }
                });
                
                request.SendWebRequest().completed += operation =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        LLMLogging.Log("Streaming request completed successfully", LogType.Log);
                        tcs.TrySetResult(true);
                    }
                    else if (request.result != UnityWebRequest.Result.ConnectionError || !cancellationToken.IsCancellationRequested)
                    {
                        string errorMessage = $"HTTP Error: {request.error}";
                        LLMLogging.Log(errorMessage, LogType.Error);
                        tcs.TrySetException(new LLMNetworkException(errorMessage));
                    }
                };

                await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                LLMLogging.Log("Operation was cancelled", LogType.Log);
                throw;
            }
            catch (Exception e)
            {
                string errorMessage = $"Exception in PostJsonStreamAsync: {e.Message}";
                LLMLogging.Log(errorMessage, LogType.Error);
                throw new LLMNetworkException(errorMessage, e);
            }
            finally
            {
                if (request != null)
                {
                    request.Dispose();
                }
            }
        }

        /// <summary>
        /// Send a GET request
        /// </summary>
        /// <param name="url">Target URL</param>
        /// <param name="apiKey">API key for authorization</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Response as string</returns>
        public static async Task<string> GetAsync(string url, string apiKey, CancellationToken cancellationToken = default)
        {
            UnityWebRequest request = null;
            try
            {
                request = UnityWebRequest.Get(url);
                LLMLogging.Log($"Sending GET request to {url}", LogType.Log);
                
                request.SetRequestHeader("Authorization", $"Bearer {apiKey}");
                
                TaskCompletionSource<string> tcs = new TaskCompletionSource<string>();

                // 注册取消回调
                cancellationToken.Register(() => 
                {
                    if (request != null && !request.isDone)
                    {
                        LLMLogging.Log("Request cancelled by user", LogType.Log);
                        request.Abort();
                        tcs.TrySetCanceled();
                    }
                });
                
                request.SendWebRequest().completed += operation =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        LLMLogging.Log("GET request completed successfully", LogType.Log);
                        tcs.TrySetResult(request.downloadHandler.text);
                    }
                    else if (request.result != UnityWebRequest.Result.ConnectionError || !cancellationToken.IsCancellationRequested)
                    {
                        string errorMessage = $"HTTP Error: {request.error}";
                        string responseContent = request.downloadHandler?.text;
                        LLMLogging.Log(errorMessage, LogType.Error);
                        
                        if (!string.IsNullOrEmpty(responseContent))
                        {
                            LLMLogging.Log($"Response content: {responseContent}", LogType.Error);
                        }

                        tcs.TrySetException(new LLMNetworkException(errorMessage+":"+responseContent));
                    }
                };

                return await tcs.Task;
            }
            catch (OperationCanceledException)
            {
                LLMLogging.Log("Operation was cancelled", LogType.Log);
                throw;
            }
            catch (Exception e)
            {
                string errorMessage = $"Exception in GetAsync: {e.Message}";
                LLMLogging.Log(errorMessage, LogType.Error);
                throw new LLMNetworkException(errorMessage, e);
            }
            finally
            {
                if (request != null)
                {
                    request.Dispose();
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
        private readonly StringBuilder buffer;
        private string lastLine = string.Empty;

        public StreamingDownloadHandler(Action<string> onData) : base()
        {
            this.onData = onData;
            this.buffer = new StringBuilder();
        }

        protected override bool ReceiveData(byte[] data, int dataLength)
        {
            try
            {
                if (data == null || dataLength == 0) return false;

                string chunk = Encoding.UTF8.GetString(data, 0, dataLength);
                buffer.Append(chunk);

                // 处理完整行
                int newlineIndex;
                while ((newlineIndex = buffer.ToString().IndexOf("\n")) != -1)
                {
                    string line = buffer.ToString(0, newlineIndex).Trim();
                    buffer.Remove(0, newlineIndex + 1);
                    
                    if (!string.IsNullOrEmpty(line))
                    {
                        lastLine = line;
                        try
                        {
                            LLMLogging.Log($"Received streaming data: {line}", LogType.Log);
                            onData?.Invoke(line);
                        }
                        catch (Exception e)
                        {
                            LLMLogging.Log($"Error in data callback: {e.Message}", LogType.Error);
                        }
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

        protected override byte[] GetData()
        {
            // 返回最后一行数据的字节数组
            return string.IsNullOrEmpty(lastLine) ? new byte[0] : Encoding.UTF8.GetBytes(lastLine);
        }

        protected override string GetText()
        {
            // 返回最后一行数据
            return lastLine;
        }

        protected override void CompleteContent()
        {
            try
            {
                // 处理剩余的buffer数据
                string remainingData = buffer.ToString().Trim();
                if (!string.IsNullOrEmpty(remainingData))
                {
                    lastLine = remainingData;
                    onData?.Invoke(remainingData);
                }
            }
            catch (Exception e)
            {
                LLMLogging.Log($"Error in CompleteContent: {e.Message}", LogType.Error);
            }
            finally
            {
                buffer.Clear();
            }
        }
    }
}
