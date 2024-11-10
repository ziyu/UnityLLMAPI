using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityLLMAPI.Models
{
    /// <summary>
    /// Manages a collection of tools that can be used by the language model
    /// </summary>
    public class ToolSet
    {
        private readonly Dictionary<string, Func<ToolCall, Task<string>>> toolHandlers = new();
        private readonly List<Tool> toolDefinitions = new();

        /// <summary>
        /// Get all registered tool definitions
        /// </summary>
        public List<Tool> Tools => new(toolDefinitions);

        /// <summary>
        /// Register a new tool
        /// </summary>
        /// <param name="tool">Tool definition</param>
        /// <param name="handler">Function to handle tool calls</param>
        public void RegisterTool(Tool tool, Func<ToolCall, Task<string>> handler)
        {
            if (tool == null) throw new ArgumentNullException(nameof(tool));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            if (string.IsNullOrEmpty(tool.function?.name))
            {
                throw new ArgumentException("Tool function name cannot be empty", nameof(tool));
            }

            string toolName = tool.function.name;
            if (toolHandlers.ContainsKey(toolName))
            {
                throw new InvalidOperationException($"Tool '{toolName}' is already registered");
            }

            toolHandlers[toolName] = handler;
            toolDefinitions.Add(tool);
        }

        /// <summary>
        /// Unregister a tool by name
        /// </summary>
        public bool UnregisterTool(string toolName)
        {
            if (string.IsNullOrEmpty(toolName)) return false;

            bool removed = toolHandlers.Remove(toolName);
            if (removed)
            {
                toolDefinitions.RemoveAll(t => t.function?.name == toolName);
            }
            return removed;
        }

        /// <summary>
        /// Clear all registered tools
        /// </summary>
        public void Clear()
        {
            toolHandlers.Clear();
            toolDefinitions.Clear();
        }

        /// <summary>
        /// Execute a tool call
        /// </summary>
        /// <returns>Tool execution result</returns>
        /// <exception cref="InvalidOperationException">Thrown when tool is not found</exception>
        public async Task<string> ExecuteTool(ToolCall toolCall)
        {
            if (toolCall == null) throw new ArgumentNullException(nameof(toolCall));
            if (string.IsNullOrEmpty(toolCall.function?.name))
            {
                throw new ArgumentException("Tool call function name cannot be empty", nameof(toolCall));
            }

            string toolName = toolCall.function.name;
            if (!toolHandlers.TryGetValue(toolName, out var handler))
            {
                throw new InvalidOperationException($"Tool '{toolName}' not found");
            }

            return await handler(toolCall);
        }

        /// <summary>
        /// Check if a tool is registered
        /// </summary>
        public bool HasTool(string toolName)
        {
            return !string.IsNullOrEmpty(toolName) && toolHandlers.ContainsKey(toolName);
        }
    }
}
