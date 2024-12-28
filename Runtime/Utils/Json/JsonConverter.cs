using System;

namespace UnityLLMAPI.Utils.Json
{
    /// <summary>
    /// Provides options for JSON serialization
    /// </summary>
    public class FormatOptions
    {
        /// <summary>
        /// Whether to serialize properties (default: false)
        /// </summary>
        public bool SerializeProperties { get; set; } = false;
    }

    /// <summary>
    /// Provides JSON serialization and deserialization with proper null handling
    /// </summary>
    public static class JsonConverter
    {
        public static readonly FormatOptions DefaultOptions = new FormatOptions();

        /// <summary>
        /// Serializes an object to JSON string using default options
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <returns>JSON string representation of the object</returns>
        public static string SerializeObject(object obj)
        {
            return SerializeObject(obj, DefaultOptions);
        }

        /// <summary>
        /// Serializes an object to JSON string with custom options
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <param name="options">Serialization options</param>
        /// <returns>JSON string representation of the object</returns>
        public static string SerializeObject(object obj, FormatOptions options)
        {
            try
            {
                return JsonSerializer.Serialize(obj, options);
            }
            catch (Exception e)
            {
                throw new JsonException($"Serialization failed: {e.Message}", e);
            }
        }

        /// <summary>
        /// Deserializes a JSON string to an object of type T
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="json">The JSON string to deserialize</param>
        /// <returns>The deserialized object</returns>
        public static T DeserializeObject<T>(string json) where T : class, new()
        {
            return DeserializeObject<T>(json, DefaultOptions);
        }

        /// <summary>
        /// Deserializes a JSON string to an object of type T with custom options
        /// </summary>
        /// <typeparam name="T">The type to deserialize to</typeparam>
        /// <param name="json">The JSON string to deserialize</param>
        /// <param name="options">Deserialization options</param>
        /// <returns>The deserialized object</returns>
        public static T DeserializeObject<T>(string json, FormatOptions options) where T : class, new()
        {
            try
            {
                return JsonDeserializer.Deserialize<T>(json, options);
            }
            catch (Exception e)
            {
                throw new JsonException($"Deserialization failed: {e.Message}", e);
            }
        }
    }
}
