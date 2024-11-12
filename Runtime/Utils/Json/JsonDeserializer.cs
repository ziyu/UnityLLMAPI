using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityLLMAPI.Utils.Json
{
    public static class JsonDeserializer
    {
        public static T Deserialize<T>(string json)
        {
            if (string.IsNullOrEmpty(json)) return default;
            
            var reader = new JsonReader(json);
            return (T)DeserializeValue(typeof(T), reader);
        }

        private static object DeserializeValue(Type type, JsonReader reader)
        {
            if (type == null)
            {
                throw new JsonException("Cannot deserialize to null type");
            }

            reader.SkipWhitespace();
            
            char current = reader.Peek();
            
            if (current == 'n' && reader.MatchNull())
            {
                return null;
            }
            
            switch (current)
            {
                case '"': 
                    if(type==typeof(string)||type==typeof(object))
                        return reader.ReadString();
                    break;
                case '{':
                    if (IsDictionaryType(type))
                    {
                        return DeserializeDictionary(type, reader);
                    }
                    return DeserializeObject(type, reader);
                case '[': 
                    if (IsListType(type))
                    {
                        return DeserializeList(type, reader);
                    }
                    return DeserializeArray(type, reader);
                case 't':
                case 'f': 
                    if(type==typeof(bool)||type==typeof(object))
                        return reader.ReadBoolean();
                    break;
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
            }
            throw new JsonException($"Unexpected character in JSON: {current}");
        }

        private static bool IsListType(Type type)
        {
            if (type == null)
            {
                return false;
            }

            try
            {
                if (type.IsGenericType)
                {
                    var genericType = type.GetGenericTypeDefinition();
                    return genericType == typeof(List<>);
                }
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }

        private static bool IsDictionaryType(Type type)
        {
            if (type == null)
            {
                return false;
            }

            try 
            {
                if (type.IsGenericType)
                {
                    var genericType = type.GetGenericTypeDefinition();
                    return genericType == typeof(Dictionary<,>) &&
                           type.GetGenericArguments()[0] == typeof(string);
                }
            }
            catch (Exception)
            {
                // 如果获取泛型类型定义失败，返回false
                return false;
            }
            return false;
        }

        private static object DeserializeList(Type type, JsonReader reader)
        {
            if (type == null)
            {
                throw new JsonException("Cannot deserialize list with null type");
            }

            reader.Expect('[');
            
            // 获取List的元素类型
            Type elementType = type.GetGenericArguments()[0];
            if (elementType == null)
            {
                throw new JsonException($"List element type cannot be null, List Type:{type}");
            }

            // 创建List实例
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = Activator.CreateInstance(listType);
            var addMethod = listType.GetMethod("Add");
            
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
                
                object element = DeserializeValue(elementType, reader);
                addMethod.Invoke(list, new[] { element });
            }
            
            return list;
        }

        private static object DeserializeDictionary(Type type, JsonReader reader)
        {
            if (type == null)
            {
                throw new JsonException("Cannot deserialize dictionary with null type");
            }

            reader.Expect('{');
            
            // 获取Dictionary的值类型参数
            Type[] genericArgs = type.GetGenericArguments();
            if (genericArgs == null || genericArgs.Length != 2)
            {
                throw new JsonException("Invalid dictionary type arguments");
            }

            Type valueType = genericArgs[1];
            if (valueType == null)
            {
                throw new JsonException("Dictionary value type cannot be null");
            }
            
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
            if (type == null)
            {
                throw new JsonException("Cannot deserialize object with null type");
            }

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
            if (type == null)
            {
                throw new JsonException("Cannot get field map for null type");
            }

            var fields = new Dictionary<string, FieldInfo>();
            foreach (var field in type.GetFields())
            {
                fields[field.Name] = field;
            }
            return fields;
        }

        private static Array DeserializeArray(Type type, JsonReader reader)
        {
            if (type == null)
            {
                throw new JsonException("Cannot deserialize array with null type");
            }

            reader.Expect('[');
            
            var elementType = type.GetElementType();
            if (elementType == null)
            {
                throw new JsonException($"Array element type cannot be null,Array Type:{type}");
            }

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
