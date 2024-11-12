using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace UnityLLMAPI.Utils.Json
{
    public static class JsonSerializer
    {
        public static string Serialize(object value)
        {
            if (value == null) return "null";

            var sb = new StringBuilder();
            SerializeValue(value, sb);
            return sb.ToString();
        }

        private static void SerializeValue(object value, StringBuilder sb)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }

            switch (value)
            {
                case string str:
                    SerializeString(str, sb);
                    break;
                case bool b:
                    sb.Append(b.ToString().ToLower());
                    break;
                case int n:
                case float f:
                case double d:
                case long l:
                    sb.Append(value.ToString());
                    break;
                case Array arr:
                    SerializeArray(arr, sb);
                    break;
                case IDictionary dict:
                    SerializeDictionary(dict, sb);
                    break;
                case IEnumerable enumerable:
                    SerializeEnumerable(enumerable, sb);
                    break;
                default:
                    if (value.GetType().IsClass)
                    {
                        SerializeObject(value, sb);
                    }
                    break;
            }
        }

        private static void SerializeString(string str, StringBuilder sb)
        {
            if (str == null)
            {
                sb.Append("null");
                return;
            }

            sb.Append('"');
            foreach (char c in str)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 32)
                        {
                            sb.Append($"\\u{(int)c:X4}");
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append('"');
        }

        private static void SerializeArray(Array arr, StringBuilder sb)
        {
            sb.Append('[');
            bool first = true;
            foreach (object item in arr)
            {
                if (!first) sb.Append(',');
                SerializeValue(item, sb);
                first = false;
            }
            sb.Append(']');
        }

        private static void SerializeEnumerable(IEnumerable enumerable, StringBuilder sb)
        {
            sb.Append('[');
            bool first = true;
            foreach (object item in enumerable)
            {
                if (!first) sb.Append(',');
                SerializeValue(item, sb);
                first = false;
            }
            sb.Append(']');
        }

        private static void SerializeDictionary(IDictionary dict, StringBuilder sb)
        {
            sb.Append('{');
            bool first = true;
            
            foreach (DictionaryEntry entry in dict)
            {
                if (!first) sb.Append(',');
                
                // 键必须是字符串类型
                if (entry.Key is string key)
                {
                    SerializeString(key, sb);
                    sb.Append(':');
                    SerializeValue(entry.Value, sb);
                    first = false;
                }
                else
                {
                    throw new JsonException("Dictionary key must be string type");
                }
            }
            
            sb.Append('}');
        }

        private static void SerializeObject(object obj, StringBuilder sb)
        {
            sb.Append('{');
            bool first = true;

            var fields = obj.GetType().GetFields(BindingFlags.Instance|BindingFlags.Public);
            foreach (var field in fields)
            {
                var value = field.GetValue(obj);
                
                // Only include the field if it's not null or if it's a non-string type
                if (value != null)
                {
                    if (!first) sb.Append(',');
                    SerializeString(field.Name, sb);
                    sb.Append(':');
                    SerializeValue(value, sb);
                    first = false;
                }
            }

            sb.Append('}');
        }
    }
}
