# Unity LLM API 文档

## 概述
Unity LLM API是一个为Unity项目提供大语言模型集成的插件。它提供了清晰的异步API设计、完善的错误处理和灵活的配置管理。主要特性包括OpenAI API集成、Tool Calling支持、流式响应、自定义JSON序列化等。

## 安装

### 使用Git URL
1. 打开Unity Package Manager (Window > Package Manager)
2. 点击左上角的"+"按钮
3. 选择"Add package from git URL"
4. 输入: `https://github.com/yourusername/UnityLLMAPI.git`

### 手动安装
1. 下载最新release
2. 解压到Unity项目的`Packages`文件夹中

## 配置

### OpenAI配置
1. 创建OpenAI配置资产:
   - 在Project窗口中右键
   - 选择 Create > UnityLLMAPI > OpenAI Configuration
   - 将其放在Resources文件夹中
2. 配置选项:
   - API Key: OpenAI API密钥
   - API Base URL: API基础URL
   - Default Model: 默认使用的模型
   - Temperature: 响应的随机性 (0.0-2.0)
   - Max Tokens: 最大生成token数
   - Enable Logging: 是否启用详细日志

## 基础用法

### 简单对话
```csharp
// 创建服务实例
var openAIService = new OpenAIService();

// 发送单条消息
string response = await openAIService.Completion("你好！");

// 发送多轮对话
var messages = new List<ChatMessage>();
messages.Add(OpenAIService.CreateSystemMessage("你是一个有帮助的助手。"));
messages.Add(OpenAIService.CreateUserMessage("你好！"));
var response = await openAIService.ChatCompletion(messages);
```

### 使用ChatbotService

### Chatbot配置

```csharp
var config = new ChatbotConfig 
{
    systemPrompt = "你是一个有帮助的助手", // 系统提示词
    defaultModel = "gpt-3.5-turbo", // 默认模型
    useStreaming = true, // 是否使用流式响应
    onStreamingChunk = (chunk) => {
        // 处理流式响应片段
    },
    toolSet = new ToolSet(...), // 工具集配置
    shouldExecuteTool = async (toolCall) => {
        // 控制是否执行工具调用
        return true;
    },
    skipToolMessage = "工具调用已跳过" // 跳过工具调用时的消息
};
```

```csharp
// 创建服务
var chatbot = new ChatbotService(new OpenAIService(), config);

// 发送消息
var response = await chatbot.SendMessage("你好！");

// 获取对话历史
var history = chatbot.Messages;

// 清除历史
chatbot.ClearHistory();
```

### 流式响应
```csharp
var config = new ChatbotConfig 
{
    useStreaming = true,
    onStreamingChunk = (chunk) => {
        // 处理每个响应片段
        Debug.Log(chunk.content);
        
        // 更新UI
        UpdateUI(chunk.content);
    }
};

var chatbot = new ChatbotService(new OpenAIService(), config);
await chatbot.SendMessage("生成一个长故事");
```

### Tool Calling

#### 定义工具
```csharp
var tools = new List<Tool> 
{
    new Tool 
    {
        name = "get_weather",
        description = "获取天气信息",
        parameters = new 
        {
            type = "object",
            properties = new 
            {
                location = new { type = "string", description = "位置" },
                date = new { type = "string", description = "日期" }
            },
            required = new[] { "location" }
        }
    },
    new Tool 
    {
        name = "search_database",
        description = "搜索数据库",
        parameters = new 
        {
            type = "object",
            properties = new 
            {
                query = new { type = "string", description = "搜索关键词" }
            },
            required = new[] { "query" }
        }
    }
};
```

#### 配置工具集
```csharp
var toolSet = new ToolSet(tools, async (toolCall) => {
    switch (toolCall.function.name) 
    {
        case "get_weather":
            var args = JsonConverter.DeserializeObject<WeatherArgs>(toolCall.function.arguments);
            return await GetWeatherData(args.location, args.date);
            
        case "search_database":
            var searchArgs = JsonConverter.DeserializeObject<SearchArgs>(toolCall.function.arguments);
            return await SearchDatabase(searchArgs.query);
            
        default:
            throw new Exception($"Unknown tool: {toolCall.function.name}");
    }
});

var config = new ChatbotConfig 
{
    toolSet = toolSet,
    shouldExecuteTool = async (toolCall) => {
        // 可以在这里添加权限检查等逻辑
        return true;
    }
};
```

#### 使用工具
```csharp
var chatbot = new ChatbotService(new OpenAIService(), config);

// 工具会在需要时自动调用
await chatbot.SendMessage("北京今天天气怎么样？");
await chatbot.SendMessage("搜索关于Unity的资料");
```

## 状态管理系统

### 状态变更事件
```csharp
// 订阅状态变更事件
chatbot.OnStateChanged += (sender, args) => {
    var oldState = args.OldState;
    var newState = args.NewState;
    var messageId = args.MessageId;
    var message = args.Message;
    
    Debug.Log($"消息 {messageId} 状态从 {oldState} 变更为 {newState}");
    
    // 根据状态变更更新UI
    UpdateUI(message, newState);
};
```

### 状态查询
```csharp
// 获取单个消息状态
var messageState = chatbot.GetMessageState(messageId);

// 获取所有特定状态的消息
var pendingMessages = chatbot.GetMessagesByState(ChatMessageState.Pending);
var failedMessages = chatbot.GetMessagesByState(ChatMessageState.Failed);

// 获取消息完整信息
var messageInfo = chatbot.GetMessageInfo(messageId);
```

### 中断恢复机制
```csharp
// 中断处理
try 
{
    await chatbot.SendMessage("生成一个长故事");
}
catch (OperationCanceledException)
{
    // 处理中断
    Debug.Log("操作已中断");
}

// 恢复处理
try 
{
    // 恢复特定消息的处理
    await chatbot.ResumeSession(messageId);
}
catch (Exception e)
{
    Debug.LogError($"恢复失败: {e.Message}");
}

// 取消恢复
var cancellationToken = new CancellationTokenSource().Token;
await chatbot.ResumeSession(messageId, cancellationToken);
```

### 会话状态同步
```csharp
// 获取会话状态
var session = chatbot.Session;

```

## 错误处理

### 错误类型
- LLMConfigurationException: 配置相关错误
- LLMValidationException: 输入验证错误
- LLMResponseException: API响应错误
- LLMException: 其他错误

### 错误处理示例
```csharp
try 
{
    await chatbot.SendMessage("你好");
} 
catch (LLMConfigurationException e) 
{
    Debug.LogError($"配置错误: {e.Message}");
    // 检查OpenAI配置是否正确
} 
catch (LLMValidationException e) 
{
    Debug.LogError($"验证错误: {e.Message}");
    // 检查输入参数
} 
catch (LLMResponseException e) 
{
    Debug.LogError($"API响应错误: {e.Message}");
    // 处理API错误，如速率限制、认证错误等
} 
catch (LLMException e) 
{
    Debug.LogError($"其他错误: {e.Message}");
    // 处理其他错误
}
```

### 日志系统
```csharp
// 在配置中启用日志
config.enableLogging = true;

// 日志会输出到Unity Console
// 包括请求详情、响应内容、错误信息等
```

## 高级功能

### 自定义JSON序列化
```csharp
// 序列化
string json = JsonConverter.SerializeObject(obj);

// 反序列化
var obj = JsonConverter.DeserializeObject<T>(json);

// 处理特殊类型
public class CustomJsonConverter : JsonConverter 
{
    public override void WriteJson(JsonWriter writer, object value)
    {
        // 自定义序列化逻辑
    }
    
    public override object ReadJson(JsonReader reader, Type objectType)
    {
        // 自定义反序列化逻辑
    }
}
```

### 自定义HTTP客户端
```csharp
// 发送POST请求
string response = await HttpClient.PostJsonAsync(url, jsonContent, apiKey);

// 处理流式响应
await HttpClient.PostJsonStreamAsync(url, jsonContent, apiKey, async (line) => {
    // 处理每行响应
});
```

## 故障排除

### 常见问题

1. "OpenAIConfig not found"
   - 确保已创建配置资产
   - 确保配置资产在Resources文件夹中

2. "API Key Invalid"
   - 检查OpenAI API密钥是否正确
   - 确认API密钥有正确的权限

3. "Request Failed"
   - 检查网络连接
   - 确认API端点可访问
   - 查看Unity Console获取详细错误信息

4. "Tool Execution Failed"
   - 检查工具定义是否正确
   - 确认工具实现逻辑无误
   - 查看工具执行的详细错误信息

5. "State Transition Failed"
   - 检查状态转换是否有效
   - 确认消息ID存在
   - 查看状态变更日志

### 性能优化

1. 消息历史管理
   - 定期清理不需要的历史消息
   - 控制消息列表的长度
   - 及时处理已完成的消息状态

2. 流式响应
   - 合理处理UI更新频率
   - 避免在每个片段都进行重量级操作
   - 使用状态缓存优化性能

3. 工具调用
   - 实现高效的工具执行逻辑
   - 合理使用shouldExecuteTool进行控制
   - 缓存频繁使用的工具结果

4. 状态管理
   - 使用高效的状态存储结构
   - 避免频繁的状态查询
   - 合理使用状态变更事件

## 支持
如有问题或功能建议，请使用GitHub issue tracker。
