using System;
using UnityEngine;

namespace UnityLLMAPI.Models
{
    /// <summary>
    /// Represents a tool/function that can be called by the language model
    /// </summary>
    [Serializable]
    public class Tool
    {
        /// <summary>
        /// The name of the tool
        /// </summary>
        public string name;

        /// <summary>
        /// Description of what the tool does
        /// </summary>
        public string description;

        /// <summary>
        /// JSON Schema object describing the parameters
        /// </summary>
        public ToolParameters parameters;
    }

    /// <summary>
    /// JSON Schema object describing the tool parameters
    /// </summary>
    [Serializable]
    public class ToolParameters
    {
        /// <summary>
        /// Type of the parameters object (always "object")
        /// </summary>
        public string type = "object";

        /// <summary>
        /// Properties of the parameters
        /// </summary>
        public ToolParameterProperty[] properties;

        /// <summary>
        /// Required property names
        /// </summary>
        public string[] required;
    }

    /// <summary>
    /// Represents a property in the tool parameters
    /// </summary>
    [Serializable]
    public class ToolParameterProperty
    {
        /// <summary>
        /// Name of the property
        /// </summary>
        public string name;

        /// <summary>
        /// Type of the property (string, number, boolean, etc.)
        /// </summary>
        public string type;

        /// <summary>
        /// Description of the property
        /// </summary>
        public string description;
    }

    /// <summary>
    /// Represents a tool call made by the model
    /// </summary>
    [Serializable]
    public class ToolCall
    {
        /// <summary>
        /// Unique identifier for this tool call
        /// </summary>
        public string id;

        /// <summary>
        /// The type of call (always "function" for now)
        /// </summary>
        public string type = "function";

        /// <summary>
        /// The function call details
        /// </summary>
        public ToolCallFunction function;
    }

    /// <summary>
    /// Details of the function being called
    /// </summary>
    [Serializable]
    public class ToolCallFunction
    {
        /// <summary>
        /// Name of the function to call
        /// </summary>
        public string name;

        /// <summary>
        /// Arguments to pass to the function, as a JSON string
        /// </summary>
        public string arguments;
    }
}
