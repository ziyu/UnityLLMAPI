using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace UnityLLMAPI.Utils.Json
{
    public static class JsonSerializer
    {
        public static string Serialize(object value, FormatOptions options = null)
        {
            if (value == null) return "null";

            var sb = new StringBuilder();
            SerializeValue(value, sb, options ?? JsonConverter.DefaultOptions);
            return sb.ToString();
        }

        private static void SerializeValue(object value, StringBuilder sb, FormatOptions options)
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
                    SerializeArray(arr, sb,options);
                    break;
                case IDictionary dict:
                    SerializeDictionary(dict, sb,options);
                    break;
                case IEnumerable enumerable:
                    SerializeEnumerable(enumerable, sb,options);
                    break;
                default:
                    if (value.GetType().IsClass)
                    {
                        SerializeObject(value, sb,options);
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

        private static void SerializeArray(Array arr, StringBuilder sb, FormatOptions options)
        {
            sb.Append('[');
            bool first = true;
            foreach (object item in arr)
            {
                if (!first) sb.Append(',');
                SerializeValue(item, sb,options);
                first = false;
            }
            sb.Append(']');
        }

        private static void SerializeEnumerable(IEnumerable enumerable, StringBuilder sb, FormatOptions options)
        {
            sb.Append('[');
            bool first = true;
            foreach (object item in enumerable)
            {
                if (!first) sb.Append(',');
                SerializeValue(item, sb,options);
                first = false;
            }
            sb.Append(']');
        }

        private static void SerializeDictionary(IDictionary dict, StringBuilder sb, FormatOptions options)
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
                    SerializeValue(entry.Value, sb,options);
                    first = false;
                }
                else
                {
                    throw new JsonException("Dictionary key must be string type");
                }
            }
            
            sb.Append('}');
        }

        private static bool IsAnonymousType(Type type)
        {
            bool hasCompilerGeneratedAttribute = type.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), false).Length > 0;
            bool nameContainsAnonymousType = type.Name.Contains("AnonymousType");
            bool isNotPublic = !type.IsPublic;
            
            return hasCompilerGeneratedAttribute && nameContainsAnonymousType && isNotPublic;
        }

        private static void SerializeObject(object obj, StringBuilder sb, FormatOptions options)
        {
            sb.Append('{');
            bool first = true;

            var type = obj.GetType();
            // Get fields and properties
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            var properties = IsAnonymousType(type) || options.SerializeProperties
                ? type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                : null;

            // Handle fields
            foreach (var field in fields)
            {
                var value = field.GetValue(obj);
                if (value != null)
                {
                    if (!first) sb.Append(',');
                    SerializeString(field.Name, sb);
                    sb.Append(':');
                    SerializeValue(value, sb, options);
                    first = false;
                }
            }

            // Handle properties if enabled
            if (properties!=null)
            {
                foreach (var property in properties)
                {
                    if (property.CanRead)
                    {
                        var value = property.GetValue(obj);
                        if (value != null)
                        {
                            if (!first) sb.Append(',');
                            SerializeString(property.Name, sb);
                            sb.Append(':');
                            SerializeValue(value, sb, options);
                            first = false;
                        }
                    }
                }
            }

            sb.Append('}');
        }
    }
}
