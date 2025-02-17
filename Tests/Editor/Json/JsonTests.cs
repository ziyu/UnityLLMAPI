using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityLLMAPI.Models;
using UnityLLMAPI.Utils.Json;

namespace UnityLLMAPI.Editor.Tests.Json
{
    public class JsonTests
    {
        [Test]
        public void TestListDeserialize()
        {
            // 测试基本类型的List
            string json = "[1,2,3,4,5]";
            var intList = JsonDeserializer.Deserialize<List<int>>(json);
            Assert.AreEqual(5, intList.Count);
            Assert.AreEqual(1, intList[0]);
            Assert.AreEqual(5, intList[4]);

            // 测试字符串List
            json = "[\"one\",\"two\",\"three\"]";
            var stringList = JsonDeserializer.Deserialize<List<string>>(json);
            Assert.AreEqual(3, stringList.Count);
            Assert.AreEqual("one", stringList[0]);
            Assert.AreEqual("three", stringList[2]);

            // 测试空List
            json = "[]";
            var emptyList = JsonDeserializer.Deserialize<List<string>>(json);
            Assert.AreEqual(0, emptyList.Count);

            // 测试null
            json = "null";
            var nullList = JsonDeserializer.Deserialize<List<string>>(json);
            Assert.IsNull(nullList);

            // 测试嵌套List
            json = "[[1,2],[3,4],[5,6]]";
            var nestedList = JsonDeserializer.Deserialize<List<List<int>>>(json);
            Assert.AreEqual(3, nestedList.Count);
            Assert.AreEqual(2, nestedList[0].Count);
            Assert.AreEqual(1, nestedList[0][0]);
            Assert.AreEqual(6, nestedList[2][1]);
        }

        [Test]
        public void TestListInvalidInput()
        {
            // 测试非数组类型的输入
            Assert.Throws<JsonException>(() =>
            {
                JsonDeserializer.Deserialize<List<int>>("{\"key\":1}");
            });

            // 测试无效的JSON
            Assert.Throws<JsonException>(() =>
            {
                JsonDeserializer.Deserialize<List<int>>("[1,2,");
            });

            // 测试类型不匹配
            Assert.Throws<JsonException>(() =>
            {
                JsonDeserializer.Deserialize<List<int>>("[\"not_a_number\"]");
            });
        }

        [Test]
        public void TestComplexListDeserialize()
        {
            // 测试包含对象的List
            var testObj = new TestObject
            {
                Name = "Test",
                Age = 25,
                Attributes = new Dictionary<string, string>
                {
                    { "city", "New York" }
                }
            };
            
            var objList = new List<TestObject> { testObj };
            string json = JsonSerializer.Serialize(objList);
            var deserializedList = JsonDeserializer.Deserialize<List<TestObject>>(json);
            
            Assert.AreEqual(1, deserializedList.Count);
            Assert.AreEqual("Test", deserializedList[0].Name);
            Assert.AreEqual(25, deserializedList[0].Age);
            Assert.AreEqual("New York", deserializedList[0].Attributes["city"]);
        }
        
        [Serializable]
        private class ChatHistory
        {
            public string chatbot_id;
            public List<ChatMessage> messages = new List<ChatMessage>();
            public long last_modified;
        }

        [Test]
        public void TestJsonDeserialize()
        {
            var testStr =
                "{\"chatbot_id\":\"UnityHelper\",\"messages\":[{\"role\":\"system\",\"content\":\"你是一个Unity开发助手\"}],\"last_modified\":1731331393}";
            var deserializedObj = JsonDeserializer.Deserialize<ChatHistory>(testStr);
            
            Assert.AreEqual("UnityHelper", deserializedObj.chatbot_id);
            Assert.AreEqual(1, deserializedObj.messages.Count);
            Assert.AreEqual("system", deserializedObj.messages[0].role);
            Assert.AreEqual(1731331393, deserializedObj.last_modified);
        }

        [Test]
        public void TestEscapeSequences()
        {
            // 测试基本转义字符
            var escapeDict = new Dictionary<string, string>
            {
                { "quote", "Hello \"World\"" },
                { "backslash", "C:\\Program Files" },
                { "newline", "Hello\nWorld" },
                { "tab", "Hello\tWorld" },
                { "return", "Hello\rWorld" },
                { "unicode", "Hello\u0020World" },
                { "complex", "\"Hello\\\\\n\tWorld\"" },
                { "control", "\b\f\n\r\t" }
            };

            string json = JsonSerializer.Serialize(escapeDict);
            var deserializedDict = JsonDeserializer.Deserialize<Dictionary<string, string>>(json);

            Assert.AreEqual("Hello \"World\"", deserializedDict["quote"]);
            Assert.AreEqual("C:\\Program Files", deserializedDict["backslash"]);
            Assert.AreEqual("Hello\nWorld", deserializedDict["newline"]);
            Assert.AreEqual("Hello\tWorld", deserializedDict["tab"]);
            Assert.AreEqual("Hello\rWorld", deserializedDict["return"]);
            Assert.AreEqual("Hello World", deserializedDict["unicode"]);
            Assert.AreEqual("\"Hello\\\\\n\tWorld\"", deserializedDict["complex"]);
            Assert.AreEqual("\b\f\n\r\t", deserializedDict["control"]);
        }

        [Test]
        public void TestInvalidEscapeSequences()
        {
            // 测试无效的转义序列
            Assert.Throws<JsonException>(() =>
            {
                JsonDeserializer.Deserialize<string>("\"\\x\"");
            });

            // 测试不完整的Unicode转义序列
            Assert.Throws<JsonException>(() =>
            {
                JsonDeserializer.Deserialize<string>("\"\\u123\"");
            });

            // 测试无效的Unicode值
            Assert.Throws<JsonException>(() =>
            {
                JsonDeserializer.Deserialize<string>("\"\\uzzzz\"");
            });

            // 测试未终止的字符串
            Assert.Throws<JsonException>(() =>
            {
                JsonDeserializer.Deserialize<string>("\"\\");
            });

            // 测试非法控制字符
            Assert.Throws<JsonException>(() =>
            {
                JsonDeserializer.Deserialize<string>("\"\u0019\"");
            });
        }

        [Test]
        public void TestDictionarySerialize()
        {
            // 测试基本类型的Dictionary
            var stringDict = new Dictionary<string, string>
            {
                { "key1", "value1" },
                { "key2", "value2" }
            };
            string json = JsonSerializer.Serialize(stringDict);
            Assert.AreEqual("{\"key1\":\"value1\",\"key2\":\"value2\"}", json);

            // 测试数字类型的Dictionary
            var numberDict = new Dictionary<string, int>
            {
                { "num1", 123 },
                { "num2", 456 }
            };
            json = JsonSerializer.Serialize(numberDict);
            Assert.AreEqual("{\"num1\":123,\"num2\":456}", json);

            // 测试混合类型的Dictionary
            var mixedDict = new Dictionary<string, object>
            {
                { "string", "text" },
                { "number", 123 },
                { "boolean", true },
                { "null", null }
            };
            json = JsonSerializer.Serialize(mixedDict);
            Assert.AreEqual("{\"string\":\"text\",\"number\":123,\"boolean\":true,\"null\":null}", json);

            // 测试空Dictionary
            var emptyDict = new Dictionary<string, string>();
            json = JsonSerializer.Serialize(emptyDict);
            Assert.AreEqual("{}", json);

            // 测试null
            Dictionary<string, string> nullDict = null;
            json = JsonSerializer.Serialize(nullDict);
            Assert.AreEqual("null", json);
        }

        [Test]
        public void TestDictionaryDeserialize()
        {
            // 测试基本类型的Dictionary
            string json = "{\"key1\":\"value1\",\"key2\":\"value2\"}";
            var stringDict = JsonDeserializer.Deserialize<Dictionary<string, string>>(json);
            Assert.AreEqual(2, stringDict.Count);
            Assert.AreEqual("value1", stringDict["key1"]);
            Assert.AreEqual("value2", stringDict["key2"]);

            // 测试数字类型的Dictionary
            json = "{\"num1\":123,\"num2\":456}";
            var numberDict = JsonDeserializer.Deserialize<Dictionary<string, int>>(json);
            Assert.AreEqual(2, numberDict.Count);
            Assert.AreEqual(123, numberDict["num1"]);
            Assert.AreEqual(456, numberDict["num2"]);

            // 测试混合类型的Dictionary
            json = "{\"string\":\"text\",\"number\":123,\"boolean\":true,\"null\":null}";
            var mixedDict = JsonDeserializer.Deserialize<Dictionary<string, object>>(json);
            Assert.AreEqual(4, mixedDict.Count);
            Assert.AreEqual("text", mixedDict["string"]);
            Assert.AreEqual(123.0, mixedDict["number"]); // JSON数字默认解析为double
            Assert.AreEqual(true, mixedDict["boolean"]);
            Assert.AreEqual(null, mixedDict["null"]);

            // 测试空Dictionary
            json = "{}";
            var emptyDict = JsonDeserializer.Deserialize<Dictionary<string, string>>(json);
            Assert.AreEqual(0, emptyDict.Count);

            // 测试null
            json = "null";
            var nullDict = JsonDeserializer.Deserialize<Dictionary<string, string>>(json);
            Assert.IsNull(nullDict);
        }

        [Test]
        public void TestDictionaryInvalidInput()
        {
            // 测试非对象类型的输入
            Assert.Throws<JsonException>(() =>
            {
                JsonDeserializer.Deserialize<Dictionary<string, string>>("[1,2,3]");
            });

            // 测试无效的JSON
            Assert.Throws<JsonException>(() =>
            {
                JsonDeserializer.Deserialize<Dictionary<string, string>>("{invalid}");
            });

            // 测试类型不匹配
            Assert.Throws<JsonException>(() =>
            {
                JsonDeserializer.Deserialize<Dictionary<string, int>>("{\"key\":\"not_a_number\"}");
            });
        }

        [Test]
        public void TestNestedDictionary()
        {
            // 测试嵌套Dictionary
            var nestedDict = new Dictionary<string, Dictionary<string, int>>
            {
                {
                    "outer1", new Dictionary<string, int>
                    {
                        { "inner1", 111 },
                        { "inner2", 222 }
                    }
                },
                {
                    "outer2", new Dictionary<string, int>
                    {
                        { "inner3", 333 },
                        { "inner4", 444 }
                    }
                }
            };

            string json = JsonSerializer.Serialize(nestedDict);
            var deserializedDict = JsonDeserializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(json);

            Assert.AreEqual(2, deserializedDict.Count);
            Assert.AreEqual(2, deserializedDict["outer1"].Count);
            Assert.AreEqual(2, deserializedDict["outer2"].Count);
            Assert.AreEqual(111, deserializedDict["outer1"]["inner1"]);
            Assert.AreEqual(222, deserializedDict["outer1"]["inner2"]);
            Assert.AreEqual(333, deserializedDict["outer2"]["inner3"]);
            Assert.AreEqual(444, deserializedDict["outer2"]["inner4"]);
        }

        // 定义一个测试类
        public class TestObject
        {
            public string Name;
            public int Age;
            public Dictionary<string, string> Attributes;

            public Dictionary<string, string> Properties { get; set; }
        }
        
        [Test]
        public void TestObjectInDictionary()
        {
            // 创建测试数据
            var dict = new Dictionary<string, object>
            {
                { "success", true },
                { "sssss", "122121" }
            };

            string json = JsonSerializer.Serialize(dict);
            var deserializedDict = JsonDeserializer.Deserialize<Dictionary<string, object>>(json);

            Assert.AreEqual(2, deserializedDict.Count);
            Assert.AreEqual(true, deserializedDict["success"]);
            Assert.AreEqual("122121", deserializedDict["sssss"]);
        }

        [Test]
        public void TestComplexObjectInDictionary()
        {
            // 创建测试数据
            var dict = new Dictionary<string, TestObject>
            {
                {
                    "person1", new TestObject
                    {
                        Name = "John",
                        Age = 30,
                        Attributes = new Dictionary<string, string>
                        {
                            { "city", "New York" },
                            { "job", "Engineer" }
                        }
                    }
                }
            };

            string json = JsonSerializer.Serialize(dict);
            var deserializedDict = JsonDeserializer.Deserialize<Dictionary<string, TestObject>>(json);

            Assert.AreEqual(1, deserializedDict.Count);
            Assert.AreEqual("John", deserializedDict["person1"].Name);
            Assert.AreEqual(30, deserializedDict["person1"].Age);
            Assert.AreEqual(2, deserializedDict["person1"].Attributes.Count);
            Assert.AreEqual("New York", deserializedDict["person1"].Attributes["city"]);
            Assert.AreEqual("Engineer", deserializedDict["person1"].Attributes["job"]);
        }

        [Test]
        public void TestComplexEscapeSequences()
        {
            // 测试复杂的转义字符组合
            var complexDict = new Dictionary<string, string>
            {
                { "nested_quotes", "He said: \"This is a \\\"quoted\\\" text\"" },
                { "mixed_escapes", "Line1\\nLine2\\tTabbed\\rReturn\\\\Backslash" },
                { "unicode_mix", "Unicode:\u0020\u0021\u0022\u005C" },
                { "all_controls", "\b\f\n\r\t\\\"/" }
            };

            string json = JsonSerializer.Serialize(complexDict);
            var deserializedDict = JsonDeserializer.Deserialize<Dictionary<string, string>>(json);

            Assert.AreEqual("He said: \"This is a \\\"quoted\\\" text\"", deserializedDict["nested_quotes"]);
            Assert.AreEqual("Line1\\nLine2\\tTabbed\\rReturn\\\\Backslash", deserializedDict["mixed_escapes"]);
            Assert.AreEqual("Unicode: !\"\\", deserializedDict["unicode_mix"]);
            Assert.AreEqual("\b\f\n\r\t\\\"/", deserializedDict["all_controls"]);
        }

        [Test]
        public void TestPropertySerialization()
        {
            var testObj = new TestObject
            {
                Name = "Test",
                Age = 25,
                Properties = new Dictionary<string, string>
                {
                    { "city", "New York" }
                }
            };

            // Test without property serialization
            var options = new FormatOptions { SerializeProperties = false };
            string json = JsonSerializer.Serialize(testObj, options);
            Assert.AreEqual("{\"Name\":\"Test\",\"Age\":25}", json);

            // Test with property serialization
            options.SerializeProperties = true;
            json = JsonSerializer.Serialize(testObj, options);
            Assert.AreEqual("{\"Name\":\"Test\",\"Age\":25,\"Properties\":{\"city\":\"New York\"}}", json);
        }

        // Test struct for serialization
        public struct TestStruct
        {
            public int Number;
            public string Text;
            public float Value;
            public Dictionary<string, int> Data;
            public bool Flag { get; set; }
        }

        [Test]
        public void TestStructSerialization()
        {
            // Create test struct
            var testStruct = new TestStruct
            {
                Number = 42,
                Text = "Hello",
                Value = 3.14f,
                Data = new Dictionary<string, int> { { "key", 100 } },
                Flag = true
            };

            // Test serialization
            string json = JsonSerializer.Serialize(testStruct);
            Assert.AreEqual("{\"Number\":42,\"Text\":\"Hello\",\"Value\":3.14,\"Data\":{\"key\":100}}", json);

            // Test with FormatOptions.SerializeProperties = true
            var options = new FormatOptions { SerializeProperties = true };
            json = JsonSerializer.Serialize(testStruct, options);
            Assert.AreEqual("{\"Number\":42,\"Text\":\"Hello\",\"Value\":3.14,\"Data\":{\"key\":100},\"Flag\":true}", json);
        }

        [Test]
        public void TestAnonymousTypeSerialization()
        {
            // 测试基本匿名类型
            var anon1 = new { Name = "Test", Value = 123 };
            string json = JsonSerializer.Serialize(anon1);
            Assert.AreEqual("{\"Name\":\"Test\",\"Value\":123}", json);

            // 测试嵌套匿名类型
            var anon2 = new
            {
                Id = 1,
                Details = new { Description = "Nested", Count = 2 }
            };
            json = JsonSerializer.Serialize(anon2);
            Assert.AreEqual("{\"Id\":1,\"Details\":{\"Description\":\"Nested\",\"Count\":2}}", json);

            // 测试包含数组的匿名类型
            var anon3 = new
            {
                Items = new[] { 1, 2, 3 },
                Info = new { Name = "ArrayTest" }
            };
            json = JsonSerializer.Serialize(anon3);
            Assert.AreEqual("{\"Items\":[1,2,3],\"Info\":{\"Name\":\"ArrayTest\"}}", json);

            // 测试空匿名类型
            var anon4 = new { };
            json = JsonSerializer.Serialize(anon4);
            Assert.AreEqual("{}", json);

            // 测试包含特殊字符的属性名
            var anon5 = new { @class = "special", @event = "test" };
            json = JsonSerializer.Serialize(anon5);
            Assert.AreEqual("{\"class\":\"special\",\"event\":\"test\"}", json);
        }
    }
}
