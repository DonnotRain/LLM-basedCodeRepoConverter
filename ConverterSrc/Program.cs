using System.ClientModel;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;

namespace LLMSmartConverter;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("欢迎使用LLM代码库智能转换工具");
        string zipFilePath = GetZipFilePath();

        Console.WriteLine("please input the target language(like C#, Java, GoLang):");
        Console.Write("请输入需要转换到的目标开发语言（如C#、Java、GoLang）：");
        string targetLanguage = Console.ReadLine();
        Console.WriteLine("please input the target framework(like ASP.NET Core+EF Core+Redis):");
        Console.Write("请输入需要转换到的目标技术栈、框架及库（如ASP.NET Core+EF Core+Redis）：");
        string targetFramework = Console.ReadLine();
        Console.WriteLine("please input the other requirements(like code standard, security standard, type mapping rules, target library, target mode, etc.):");
        Console.Write("请输入其他的转换要求及说明（如代码规范、安全标准、类型映射规则、目标库、目标模式等）：");
        string otherRequirements = Console.ReadLine();

        //尝试从参数中解析LLM供应商类型、API Key
        string llmProvider = GetLlmProvider();
        string llmApiKey = GetLlmApiKey();

        await CodeConverter.Analyse(zipFilePath, targetLanguage, targetFramework, otherRequirements, llmProvider, llmApiKey);
    }

    private static string GetZipFilePath()
    {
        Console.WriteLine("please input the zip file path(ensure the zip file root directory has a readme.md file):");
        Console.Write("请输入需要转换的代码库zip文件路径(确保压缩包根目录有readme.md文件)：");
        string zipFilePath = Console.ReadLine();
        if (!File.Exists(zipFilePath))
        {
            Console.WriteLine("文件不存在，请重新输入！");
            return GetZipFilePath();
        }
        return zipFilePath;
    }

    //获取输入的llmProvider
    private static string GetLlmProvider()
    {
        Console.WriteLine("please input the llm provider(1 for volcengine, 2 for deepseek, 3 for tencent):");
        Console.Write("请输入LLM供应商类型（1对应火山引擎、2对应深度求索官方、3对应腾讯云）：");
        string llmProvider = Console.ReadLine();
        if (string.IsNullOrEmpty(llmProvider))
        { 
            Console.WriteLine("input is null or empty, please input again!");
            Console.WriteLine("输入不能为空，请重新输入！");
            return GetLlmProvider();
        }
        if (llmProvider != "1" && llmProvider != "2" && llmProvider != "3")
        {
            Console.WriteLine("input is not correct, please input again!");
            Console.WriteLine("输入的LLM供应商类型不正确，请重新输入！");
            return GetLlmProvider();
        }
        return llmProvider;
    }

    //获取输入的llmApiKey
    private static string GetLlmApiKey()
    {
        Console.WriteLine("please input the llm api key:");
        Console.Write("请输入LLM API Key：");
        string llmApiKey = Console.ReadLine();
        if (string.IsNullOrEmpty(llmApiKey))
        {
            Console.WriteLine("input is null or empty, please input again!");
            Console.WriteLine("输入不能为空，请重新输入！");
            return GetLlmApiKey();
        }
        return llmApiKey;
    }

}