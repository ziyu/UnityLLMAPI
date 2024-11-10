using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityLLMAPI.Models
{
    /// <summary>
    /// Represents a tool that can be called by the language model
    /// </summary>
    [Serializable]
    public class Tool
    {
        /// <summary>
        /// The type of the tool (currently only "function" is supported)
        /// </summary>
        public string type = "function";

        /// <summary>
        /// The function details
        /// </summary>
        public ToolFunction function;
    }

    /// <summary>
    /// Represents the function details of a tool
    /// </summary>
    [Serializable]
    public class ToolFunction
    {
        /// <summary>
        /// The name of the function
        /// </summary>
        public string name;

        /// <summary>
        /// Description of what the function does
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
        public Dictionary<string,ToolParameterProperty> properties;

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
        /// Type of the property (string, number, boolean, etc.)
        /// </summary>
        public string type;

        /// <summary>
        /// Description of the property
        /// </summary>
        public string description;

        /// <summary>
        /// Enumeration of allowed values (optional)
        /// </summary>
        public string[] @enum;
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
