using System.ClientModel;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;
using Newtonsoft.Json;
using LLMSmartConverter.Models;
using System.Text.RegularExpressions;

namespace LLMSmartConverter;

public class ModuleAnalyse
{
    //0.系统级需求：体育场运营管理系统
    //1.模块需求：用户管理
    //3.功能需求：用户新增

    //2.模块需求：票务管理
    //3.模块需求：场馆管理
    //4.模块需求：赛事管理
    //5.模块需求：财务管理
    //6.模块需求：安全管理
    //7.模块需求：数据分析
    //8.模块需求：系统管理

    public static async Task Analyse(string systemRequirement)
    {
        OpenAIClient api = new(new ApiKeyCredential("sk-e371b9de08b1422ca1a3c5eb3a0e0c2e"), new OpenAIClientOptions()
        {
            Endpoint = new Uri("https://api.deepseek.com/v1"),
        });
        var client = api.GetChatClient("deepseek-chat");
        // var client = new OpenAIClient("sk-proj-1234567890");
        // ChatClient client = new(model: "deepseek-reasoner", apiKey: "sk-a71a1425915047bbbd8975cff36d3609");

        string systemText = """
       你是个业务领域专家，协助用户分析系统级别的软件需求，拆分成软件系统的功能模块，以JSON的形式输出，输出的 JSON 需遵守以下的格式：
    {
    "name": <模块名称>,
    "background": <模块背景>,
    "description": <模块详细描述>,
    "priority": <模块优先级:高=1、中=2、低=3>,
    "remarks": <模块备注>,
    "app": <应用端>
    }
    ；其中的应用端是指你规划出的应用（如管理网页端、用户网页端、小程序端、H5端等）。
    """;
        var systemMessage = ChatMessage.CreateSystemMessage(systemText);

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

        var userChatMessage = ChatMessage.CreateUserMessage(systemRequirement);

        ChatMessage[] chatMessages = [systemMessage, userChatMessage];
        var options = new ChatCompletionOptions
        {
            Temperature = 0.5f,
            TopLogProbabilityCount = null,
        };

        using StreamWriter writer = new($"readme{DateTime.Now.Ticks}.md", false);
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
        // 使用正则表达式去除markdown的json标记及前后多余内容
        string cleanJson = Regex.Replace(allccontent, @"```json\s*([\s\S]*?)\s*```", "$1").Trim();
        var json = JsonConvert.DeserializeObject<List<SystemRequirementModule>>(cleanJson);
        //写入到JSON文件
        string jsonFilePath = $"readme{DateTime.Now.Ticks}.json";
        File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(json, Formatting.Indented));
        Console.WriteLine($"ChatFinished");
        // Console.WriteLine($"[完整方案]: {allccontent}");
        // Console.ReadLine();
    }
}