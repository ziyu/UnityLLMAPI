using System.Threading.Tasks;
using System.Collections.Generic;

namespace UnityLLMAPI.Interfaces
{
    /// <summary>
    /// Interface for Language Model services
    /// </summary>
    public interface ILLMService
    {
        /// <summary>
        /// Send a chat completion request to the language model
        /// </summary>
        /// <param name="messages">List of chat messages</param>
        /// <param name="model">Model name to use</param>
        /// <returns>Response from the language model</returns>
        Task<string> ChatCompletion(List<ChatMessage> messages, string model = "gpt-3.5-turbo");

        /// <summary>
        /// Send a completion request to the language model
        /// </summary>
        /// <param name="prompt">Input prompt</param>
        /// <param name="model">Model name to use</param>
        /// <returns>Response from the language model</returns>
        Task<string> Completion(string prompt, string model = "text-davinci-003");
    }
}
