using System.Collections.Generic;
using NUnit.Framework;
using UnityLLMAPI.Utils.Json;

namespace UnityLLMAPI.Editor.Tests.Json
{
    public class JsonTests
    {
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
            public Dictionary<string, string> Properties;
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
                        Properties = new Dictionary<string, string>
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
            Assert.AreEqual(2, deserializedDict["person1"].Properties.Count);
            Assert.AreEqual("New York", deserializedDict["person1"].Properties["city"]);
            Assert.AreEqual("Engineer", deserializedDict["person1"].Properties["job"]);
        }
    }
}
