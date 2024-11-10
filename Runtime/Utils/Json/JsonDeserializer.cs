using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityLLMAPI.Utils.Json
{
    public static class JsonDeserializer
    {
        public static T Deserialize<T>(string json) where T : class, new()
        {
            if (string.IsNullOrEmpty(json)) return null;
            
            var reader = new JsonReader(json);
            return (T)DeserializeValue(typeof(T), reader);
        }

        private static object DeserializeValue(Type type, JsonReader reader)
        {
            reader.SkipWhitespace();
            
            char current = reader.Peek();
            
            if (current == 'n' && reader.MatchNull())
            {
                return null;
            }
            
            switch (current)
            {
                case '"': return reader.ReadString();
                case '{':
                    if (IsDictionaryType(type))
                    {
                        return DeserializeDictionary(type, reader);
                    }
                    return DeserializeObject(type, reader);
                case '[': return DeserializeArray(type, reader);
                case 't':
                case 'f': return reader.ReadBoolean();
                case '-':
                case '0':
                case '1':
                case '2':
                case '3':
                case '4':
                case '5':
                case '6':
                case '7':
                case '8':
                case '9':
                    if (type == typeof(int)) return (int)reader.ReadNumber();
                    if (type == typeof(long)) return (long)reader.ReadNumber();
                    if (type == typeof(float)) return (float)reader.ReadNumber();
                    return reader.ReadNumber();
                default:
                    throw new JsonException($"Unexpected character in JSON: {current}");
            }
        }

        private static bool IsDictionaryType(Type type)
        {
            if (type.IsGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();
                return genericType == typeof(Dictionary<,>) &&
                       type.GetGenericArguments()[0] == typeof(string);
            }
            return false;
        }

        private static object DeserializeDictionary(Type type, JsonReader reader)
        {
            reader.Expect('{');
            
            // 获取Dictionary的值类型参数
            Type[] genericArgs = type.GetGenericArguments();
            Type valueType = genericArgs[1];
            
            // 创建Dictionary实例
            var dictionaryType = typeof(Dictionary<,>).MakeGenericType(typeof(string), valueType);
            var dictionary = Activator.CreateInstance(dictionaryType);
            var addMethod = dictionaryType.GetMethod("Add");
            
            while (true)
            {
                reader.SkipWhitespace();
                if (reader.Peek() == '}')
                {
                    reader.Read();
                    break;
                }
                
                if (reader.Peek() == ',')
                {
                    reader.Read();
                    continue;
                }
                
                string key = reader.ReadString();
                reader.Expect(':');
                
                object value = DeserializeValue(valueType, reader);
                addMethod.Invoke(dictionary, new[] { key, value });
            }
            
            return dictionary;
        }

        private static object DeserializeObject(Type type, JsonReader reader)
        {
            reader.Expect('{');
            
            var result = Activator.CreateInstance(type);
            var fields = GetFieldMap(type);
            
            while (true)
            {
                reader.SkipWhitespace();
                if (reader.Peek() == '}')
                {
                    reader.Read();
                    break;
                }
                
                if (reader.Peek() == ',')
                {
                    reader.Read();
                    continue;
                }
                
                string fieldName = reader.ReadString();
                reader.Expect(':');
                
                if (fields.TryGetValue(fieldName, out FieldInfo field))
                {
                    object value = DeserializeValue(field.FieldType, reader);
                    field.SetValue(result, value);
                }
                else
                {
                    reader.SkipValue();
                }
            }
            
            return result;
        }

        private static Dictionary<string, FieldInfo> GetFieldMap(Type type)
        {
            var fields = new Dictionary<string, FieldInfo>();
            foreach (var field in type.GetFields())
            {
                fields[field.Name] = field;
            }
            return fields;
        }

        private static Array DeserializeArray(Type type, JsonReader reader)
        {
            reader.Expect('[');
            
            var elementType = type.GetElementType();
            var elements = new List<object>();
            
            while (true)
            {
                reader.SkipWhitespace();
                if (reader.Peek() == ']')
                {
                    reader.Read();
                    break;
                }
                
                if (reader.Peek() == ',')
                {
                    reader.Read();
                    continue;
                }
                
                elements.Add(DeserializeValue(elementType, reader));
            }
            
            Array array = Array.CreateInstance(elementType, elements.Count);
            for (int i = 0; i < elements.Count; i++)
            {
                array.SetValue(elements[i], i);
            }
            
            return array;
        }
    }
}
