using System.ClientModel;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;

namespace LLMSmartConverter;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Hello, .NET 9!");
        await CodeConverter.Analyse("景区票务系统");
    }

    private static async Task Test()
    {
        OpenAIClient api = new(new ApiKeyCredential("sk-a71a1425915047bbbd8975cff36d3609"), new OpenAIClientOptions()
        {
            Endpoint = new Uri("https://api.deepseek.com/v1"),
        });
        var client = api.GetChatClient("deepseek-coder");
        // var client = new OpenAIClient("sk-proj-1234567890");
        // ChatClient client = new(model: "deepseek-reasoner", apiKey: "sk-a71a1425915047bbbd8975cff36d3609");

        string text = """
        作为资深系统分析师，请将用户需求转换为结构化JSON格式：
    {
    "功能模块": [{"名称":"","描述":"","输入数据":"","输出数据":""}],
    "数据实体": [{"实体名":"","属性":[],"关系":""}],
    "业务规则": ["规则描述"]
    }
    要求：属性需标注数据类型，关系使用箭头语法(如User->Order)
    """;

// string text = """
//     作为资深系统分析师，请将用户需求转换为结构化JSON格式：
//     {
//       "功能模块": [{"名称":"","描述":"","输入数据":"","输出数据":""}],
//       "数据实体": [{"实体名":"","属性":[],"关系":""}],
//       "业务规则": ["规则描述"]
//     }
//     要求：属性需标注数据类型，关系使用箭头语法(如User->Order)
//     """;  // 闭合符缩进 4 空格

        var systemModuleChatMessage = ChatMessage.CreateSystemMessage("你是个业务领域专家，协助用户进行需求分析并转化成详细的可执行的软件需求，需要将需求整理后拆分成具体的应用端（如管理网页端、用户网页端、小程序端、H5端等）及对应页面，包含详细的页面布局、页面内容、页面交互。");

        var systemChatMessage = ChatMessage.CreateSystemMessage("你是个业务领域专家，协助用户进行需求分析并转化成详细的可执行的软件需求，需要具体到每个模块每个页面，甚至包含详细的页面布局、页面内容、页面交互、页面数据、页面样式等，并给出详细的开发计划。");
        
        var userChatMessage = ChatMessage.CreateUserMessage("景区票务系统");

        ChatMessage[] chatMessages = [systemModuleChatMessage, userChatMessage];
        var options = new ChatCompletionOptions
        {
            Temperature = 0.5f,
            TopLogProbabilityCount = null,
        };

        using StreamWriter writer = new("readme1.md", false);
        string allccontent = "";
        await foreach (StreamingChatCompletionUpdate delta in client.CompleteChatStreamingAsync(chatMessages, options))
        {
            if (delta.ContentUpdate.Count == 0) continue;

            var content = delta.ContentUpdate[0].Text;
            await writer.WriteAsync(content);
            allccontent += content;
            Console.WriteLine($"[ASSISTANT]: {content}, FinishReason: {delta.FinishReason}, Usage: {delta.Usage}");
        }
        await writer.FlushAsync();

        Console.WriteLine($"ChatFinished");
        Console.WriteLine($"[完整方案]: {allccontent}");
        // Console.ReadLine();
    }
}