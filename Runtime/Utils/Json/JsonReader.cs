using System;
using System.Text;

namespace UnityLLMAPI.Utils.Json
{
    internal class JsonReader
    {
        private readonly string json;
        private int position;

        public JsonReader(string json)
        {
            this.json = json;
            this.position = 0;
        }

        public char Peek()
        {
            if (position >= json.Length)
                throw new JsonException("Unexpected end of JSON input");
            return json[position];
        }

        public char Read()
        {
            if (position >= json.Length)
                throw new JsonException("Unexpected end of JSON input");
            return json[position++];
        }

        public void Expect(char expected)
        {
            char c = Read();
            if (c != expected)
                throw new JsonException($"Expected '{expected}', got '{c}'");
        }

        public void SkipWhitespace()
        {
            while (position < json.Length && char.IsWhiteSpace(json[position]))
                position++;
        }

        private string ProcessEscapeSequence()
        {
            char c = Read();
            switch (c)
            {
                case '"': return "\"";
                case '\\': return "\\";
                case '/': return "/";
                case 'b': return "\b";
                case 'f': return "\f";
                case 'n': return "\n";
                case 'r': return "\r";
                case 't': return "\t";
                case 'u':
                    // 读取4位十六进制数
                    if (position + 4 > json.Length)
                        throw new JsonException("Incomplete Unicode escape sequence");
                    
                    string hex = "";
                    for (int i = 0; i < 4; i++)
                    {
                        char hexChar = Read();
                        if (!IsHexDigit(hexChar))
                            throw new JsonException($"Invalid Unicode escape sequence: \\u{hex}{hexChar}");
                        hex += hexChar;
                    }
                    
                    try
                    {
                        int value = Convert.ToInt32(hex, 16);
                        return char.ConvertFromUtf32(value);
                    }
                    catch (Exception)
                    {
                        throw new JsonException($"Invalid Unicode escape value: \\u{hex}");
                    }
                default:
                    throw new JsonException($"Invalid escape sequence: \\{c}");
            }
        }

        private bool IsHexDigit(char c)
        {
            return (c >= '0' && c <= '9') ||
                   (c >= 'a' && c <= 'f') ||
                   (c >= 'A' && c <= 'F');
        }

        public string ReadString()
        {
            Expect('"');
            var sb = new StringBuilder();
            
            while (position < json.Length)
            {
                char c = Read();
                
                if (c == '"')
                    return sb.ToString();
                
                if (c == '\\')
                {
                    sb.Append(ProcessEscapeSequence());
                    continue;
                }
                
                // 检查非法字符
                if (c < 0x20)
                    throw new JsonException($"Invalid control character in string: {(int)c}");
                
                sb.Append(c);
            }
            
            throw new JsonException("Unterminated string");
        }

        public bool ReadBoolean()
        {
            if (MatchLiteral("true")) return true;
            if (MatchLiteral("false")) return false;
            throw new JsonException("Expected 'true' or 'false'");
        }

        public bool MatchNull()
        {
            return MatchLiteral("null");
        }

        private bool MatchLiteral(string literal)
        {
            if (position + literal.Length > json.Length) return false;
            
            for (int i = 0; i < literal.Length; i++)
            {
                if (json[position + i] != literal[i]) return false;
            }
            
            position += literal.Length;
            return true;
        }

        public double ReadNumber()
        {
            var sb = new StringBuilder();
            
            // Read optional minus sign
            if (Peek() == '-')
            {
                sb.Append(Read());
            }
            
            // Read digits before decimal point
            if (!char.IsDigit(Peek()))
                throw new JsonException("Invalid number format: digit expected");
                
            while (position < json.Length && char.IsDigit(Peek()))
            {
                sb.Append(Read());
            }
            
            // Read optional decimal point and following digits
            if (position < json.Length && Peek() == '.')
            {
                sb.Append(Read());
                if (!char.IsDigit(Peek()))
                    throw new JsonException("Invalid number format: digit expected after decimal point");
                    
                while (position < json.Length && char.IsDigit(Peek()))
                {
                    sb.Append(Read());
                }
            }
            
            // Read optional exponent
            if (position < json.Length && (Peek() == 'e' || Peek() == 'E'))
            {
                sb.Append(Read());
                if (Peek() == '+' || Peek() == '-')
                {
                    sb.Append(Read());
                }
                if (!char.IsDigit(Peek()))
                    throw new JsonException("Invalid number format: digit expected in exponent");
                    
                while (position < json.Length && char.IsDigit(Peek()))
                {
                    sb.Append(Read());
                }
            }
            
            if (double.TryParse(sb.ToString(), out double result))
                return result;
                
            throw new JsonException($"Invalid number format: {sb}");
        }

        public void SkipValue()
        {
            SkipWhitespace();
            char current = Peek();
            
            switch (current)
            {
                case '"':
                    ReadString();
                    break;
                case '{':
                    SkipObject();
                    break;
                case '[':
                    SkipArray();
                    break;
                case 't':
                case 'f':
                    ReadBoolean();
                    break;
                case 'n':
                    MatchNull();
                    break;
                default:
                    ReadNumber();
                    break;
            }
        }

        public void SkipObject()
        {
            Expect('{');
            SkipWhitespace();
            
            if (Peek() == '}')
            {
                Read();
                return;
            }
            
            while (true)
            {
                SkipWhitespace();
                ReadString(); // Skip key
                SkipWhitespace();
                Expect(':');
                SkipWhitespace();
                SkipValue();
                SkipWhitespace();
                
                if (Peek() == '}')
                {
                    Read();
                    break;
                }
                
                Expect(',');
            }
        }

        public void SkipArray()
        {
            Expect('[');
            SkipWhitespace();
            
            if (Peek() == ']')
            {
                Read();
                return;
            }
            
            while (true)
            {
                SkipWhitespace();
                SkipValue();
                SkipWhitespace();
                
                if (Peek() == ']')
                {
                    Read();
                    break;
                }
                
                Expect(',');
            }
        }
    }
}
