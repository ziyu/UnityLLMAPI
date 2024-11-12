# Unity LLM API

一个用于在Unity中集成OpenAI等大语言模型API的插件。提供了简洁的API设计、完善的错误处理和灵活的配置管理。

## 特性

- 支持OpenAI Chat Completion API
- Tool Calling功能支持
- 流式响应支持
- 异步操作
- 完善的错误处理
- 可配置的API参数
- 对话历史管理
- 自定义JSON序列化
- 状态管理系统
  - 实时状态追踪
  - 状态变更事件通知
  - 会话级别状态同步
  - 中断恢复机制

## 安装

1. 打开Unity Package Manager
2. 点击左上角的"+"按钮
3. 选择"Add package from git URL"
4. 输入: `https://github.com/yourusername/UnityLLMAPI.git`

## 配置

1. 创建OpenAI配置资产:
   - 在Project窗口中右键
   - 选择 Create > UnityLLMAPI > OpenAI Configuration
   - 将其放在Resources文件夹中
2. 配置你的OpenAI API密钥
3. (可选) 调整其他设置:
   - 默认模型
   - 温度
   - 最大token数
   - 日志级别

## 基础用法

### 简单对话
```csharp
// 创建服务实例
var openAIService = new OpenAIService();

// 创建消息列表
var messages = new List<ChatMessage>();
messages.Add(OpenAIService.CreateSystemMessage("你是一个有帮助的助手。"));
messages.Add(OpenAIService.CreateUserMessage("你好！"));

// 发送请求
string response = await openAIService.ChatCompletion(messages);
```

### 流式响应
```csharp
// 创建配置
var config = new ChatbotConfig 
{
    useStreaming = true,
    onStreamingChunk = (chunk) => {
        Debug.Log(chunk.content); // 实时显示响应内容
    }
};

// 创建服务
var chatbot = new ChatbotService(new OpenAIService(), config);

// 发送消息
await chatbot.SendMessage("生成一个长故事");
```

### Tool Calling
```csharp
// 定义工具
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
                location = new { type = "string", description = "位置" }
            },
            required = new[] { "location" }
        }
    }
};

// 创建配置
var config = new ChatbotConfig 
{
    toolSet = new ToolSet(tools, async (toolCall) => {
        // 实现工具调用逻辑
        return "晴天，25度";
    })
};

// 创建服务
var chatbot = new ChatbotService(new OpenAIService(), config);

// 发送可能触发工具调用的消息
await chatbot.SendMessage("北京今天天气怎么样？");
```

### 状态管理
```csharp
// 订阅状态变更事件
chatbot.OnStateChanged += (sender, args) => {
    Debug.Log($"状态变更: {args.OldState} -> {args.NewState}");
    Debug.Log($"消息ID: {args.MessageId}");
};

// 获取消息状态
var messageState = chatbot.GetMessageState(messageId);

// 获取所有pending消息
var pendingMessages = chatbot.GetPendingMessages();

// 中断恢复
await chatbot.ResumeAsync(messageId);
```

## 示例

查看Samples文件夹获取完整示例:
- 基础聊天实现
- UI集成
- Tool Calling示例
- 流式响应演示
- 状态管理示例

## 错误处理

```csharp
try 
{
    await chatbot.SendMessage("你好");
} 
catch (LLMConfigurationException e) 
{
    // 处理配置错误
} 
catch (LLMValidationException e) 
{
    // 处理验证错误
} 
catch (LLMResponseException e) 
{
    // 处理API响应错误
} 
catch (LLMException e) 
{
    // 处理其他错误
}
```

## 许可

本项目基于MIT许可证开源 - 详见LICENSE.md文件
