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

        public string ReadString()
        {
            Expect('"');
            var sb = new StringBuilder();
            
            while (true)
            {
                char c = Read();
                if (c == '"') break;
                
                if (c == '\\')
                {
                    c = Read();
                    switch (c)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            string hex = new string(new[] { Read(), Read(), Read(), Read() });
                            sb.Append((char)Convert.ToInt32(hex, 16));
                            break;
                        default:
                            throw new JsonException($"Invalid escape sequence: \\{c}");
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            
            return sb.ToString();
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
            while (position < json.Length && char.IsDigit(Peek()))
            {
                sb.Append(Read());
            }
            
            // Read optional decimal point and following digits
            if (position < json.Length && Peek() == '.')
            {
                sb.Append(Read());
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
                while (position < json.Length && char.IsDigit(Peek()))
                {
                    sb.Append(Read());
                }
            }
            
            return double.Parse(sb.ToString());
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
            int depth = 1;
            
            while (depth > 0)
            {
                char c = Read();
                if (c == '{') depth++;
                else if (c == '}') depth--;
                else if (c == '"') SkipString();
            }
        }

        public void SkipArray()
        {
            Expect('[');
            int depth = 1;
            
            while (depth > 0)
            {
                char c = Read();
                if (c == '[') depth++;
                else if (c == ']') depth--;
                else if (c == '"') SkipString();
            }
        }

        private void SkipString()
        {
            while (true)
            {
                char c = Read();
                if (c == '\\') Read();
                else if (c == '"') break;
            }
        }
    }
}
