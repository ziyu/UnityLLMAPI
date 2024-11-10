using System;

namespace UnityLLMAPI.Utils.Json
{
    /// <summary>
    /// Provides JSON serialization and deserialization with proper null handling
    /// </summary>
    public static class JsonConverter
    {
        /// <summary>
        /// Serializes an object to JSON string, properly handling null values
        /// </summary>
        /// <param name="obj">The object to serialize</param>
        /// <returns>JSON string representation of the object</returns>
        public static string SerializeObject(object obj)
        {
            try
            {
                return JsonSerializer.Serialize(obj);
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
            try
            {
                return JsonDeserializer.Deserialize<T>(json);
            }
            catch (Exception e)
            {
                throw new JsonException($"Deserialization failed: {e.Message}", e);
            }
        }
    }
}
