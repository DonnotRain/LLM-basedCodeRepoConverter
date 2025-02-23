using LLMSmartConverter.Models;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Newtonsoft.Json;
using OpenAI.Chat;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace LLMSmartConverter;

//This is a code converter that converts code from one language to another.
public class CodeConverter
{
    private readonly LLMClientFactory _clientFactory;

    public CodeConverter(ILLMClientConfig config)
    {
        _clientFactory = new LLMClientFactory(config);
    }

    public static async Task Analyse(string zipFilePath, string targetLanguage, string targetFramework, string otherRequirements, string llmProvider, string llmApiKey)
    {
        ILLMClientConfig config;
        if (llmProvider == "1")
        {
            config = new VolcengineConfig(llmApiKey);
        }
        else if (llmProvider == "2")
        {
            config = new DeepSeekConfig(llmApiKey);
        }
        else if (llmProvider == "3")
        {
            config = new TencentLKEAPConfig(llmApiKey);
        }
        else
        {
            Console.WriteLine("未支持的LLM供应商类型，请重新输入！");
            return;
        }
        var converter = new CodeConverter(config);
        await converter.AnalyseInternal(zipFilePath, targetLanguage, targetFramework, otherRequirements);
    }

    private async Task AnalyseInternal(string zipFilePath, string targetLanguage, string targetFramework, string otherRequirements)
    {
        var client = _clientFactory.CreateChatClient();
        var assessmentClient = _clientFactory.CreateChatClient(true);

        //先调用DeepSeek-R1进行技术栈评估
        using StreamReader assessmentReader = new(typeof(CodeConverter).Assembly.GetManifestResourceStream("LLMSmartConverter.templates.assessmentSystemPrompt.md") ?? throw new InvalidOperationException("assessmentSystemPrompt.md not found"));
        string assessmentSystem = await ReadAndReplacePromp(targetLanguage, targetFramework, otherRequirements, assessmentReader);
        var assessmentSystemMessage = ChatMessage.CreateSystemMessage(assessmentSystem);

        //从嵌入的资源：reasoningSystemPrompt.md 读取提示词模板 
        using StreamReader reader = new(typeof(CodeConverter).Assembly.GetManifestResourceStream("LLMSmartConverter.templates.reasoningSystemPrompt.md") ?? throw new InvalidOperationException("reasoningSystemPrompt.md not found"));
        string systemPromptText = await ReadAndReplacePromp(targetLanguage, targetFramework, otherRequirements, reader);

        var systemMessage = ChatMessage.CreateSystemMessage(systemPromptText);

        //从压缩包中读取文件结构     
        string fileFullName = zipFilePath;
        //解压压缩包
        var unzipPath = Path.Combine(Path.GetDirectoryName(fileFullName), Path.GetFileNameWithoutExtension(fileFullName));
        if (Directory.Exists(unzipPath))
        {
            Directory.Delete(unzipPath, true);
        }

        ZipFile.ExtractToDirectory(fileFullName, unzipPath, true);

        string fileStructureString = ParseFolderStructure(unzipPath);
        string structureString = "以下为原项目文件结构：" + Environment.NewLine + fileStructureString;
        structureString += Environment.NewLine;

        //读取readme.md文件内容 
        string readmeFilePath = Path.Combine(unzipPath, "readme.md");
        if (File.Exists(readmeFilePath))
        {
            structureString += Environment.NewLine;
            structureString += "以下为原项目的readme.md文件内容：" + Environment.NewLine + File.ReadAllText(readmeFilePath);
        }

        //转换后的文件保存路径 
        var convertFilePath = Path.Combine(Path.GetDirectoryName(fileFullName), Path.GetFileNameWithoutExtension(fileFullName) + "_converted" + DateTime.Now.Ticks);
        //确保文件夹存在
        if (Directory.Exists(convertFilePath)) Directory.Delete(convertFilePath, true);
        if (!Directory.Exists(convertFilePath))
        {
            Directory.CreateDirectory(convertFilePath);
        }

        Console.WriteLine($"转换后的文件保存路径: {convertFilePath}");
        using StreamWriter allNoticeWriter = new(Path.Combine(convertFilePath, "allNotice.md"), false)
        {
            AutoFlush = true
        };
        var userChatMessage = ChatMessage.CreateUserMessage(structureString);

        var options = new ChatCompletionOptions
        {
            Temperature = 0.5f,
            TopLogProbabilityCount = null,
        };

        //先进行技术栈评估，切换DeepSeek-R1模型
        ChatMessage[] assessmentMessages = [assessmentSystemMessage, userChatMessage];
        using StreamWriter assessmentWriter = new(Path.Combine(convertFilePath, $"assessment{DateTime.Now.Ticks}.md"), false);
        string assessmentContent = "";
        Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "技术栈评估");
        await foreach (StreamingChatCompletionUpdate delta in assessmentClient.CompleteChatStreamingAsync(assessmentMessages, options))
        {
            if (delta.ContentUpdate.Count == 0) continue;
            var content = delta.ContentUpdate[0].Text;
            await assessmentWriter.WriteAsync(content);
            assessmentContent += content;
            Console.Write(content);
            // Console.WriteLine($"[ASSISTANT]: {content}, FinishReason: {delta.FinishReason}, Usage: {delta.Usage}");
        }
        await assessmentWriter.FlushAsync();
        Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "技术栈评估完成");
        Console.WriteLine($"[完整评估回复]: {assessmentContent}");

        var assessmentChatMessage = ChatMessage.CreateAssistantMessage(assessmentContent);

        ChatMessage[] chatMessages = [systemMessage, assessmentChatMessage, userChatMessage];

        using StreamWriter writer = new(Path.Combine(convertFilePath, $"moduleReadme{DateTime.Now.Ticks}.md"), false);
        string moduleAnalyseContent = "";
        Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "模块拆分");
        await foreach (StreamingChatCompletionUpdate delta in assessmentClient.CompleteChatStreamingAsync(chatMessages, options))
        {
            if (delta.ContentUpdate.Count == 0) continue;
            var content = delta.ContentUpdate[0].Text;
            await writer.WriteAsync(content);
            moduleAnalyseContent += content;
            Console.Write(content);
            // Console.WriteLine($"[ASSISTANT]: {content}, FinishReason: {delta.FinishReason}, Usage: {delta.Usage}");
        }
        await writer.FlushAsync();
        Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + $"[拆分完成]");

        //助手回复的消息
        var assistantMessage = ChatMessage.CreateAssistantMessage(moduleAnalyseContent);

        // 解析markdown内容
        var markdownDocument = Markdown.Parse(moduleAnalyseContent);
        var codeFileModules = new List<CodeFileModule>();

        // 遍历markdown中的代码块
        foreach (var node in markdownDocument.Descendants().OfType<FencedCodeBlock>())
        {
            // 找到json代码块
            if (node.Info?.Equals("json", StringComparison.OrdinalIgnoreCase) == true)
            {
                try
                {
                    // 解析json内容为CodeFileModule列表
                    var modules = JsonConvert.DeserializeObject<List<CodeFileModule>>(string.Join("\n", node.Lines.Lines));
                    if (modules != null)
                    {
                        codeFileModules.AddRange(modules);
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"解析JSON内容失败: {ex.Message}");
                }
            }
        }

        //从嵌入的资源：reasoningSystemPrompt.md 读取提示词模板 
        using StreamReader promptReader = new(typeof(CodeConverter).Assembly.GetManifestResourceStream("LLMSmartConverter.templates.systemPrompt.md") ?? throw new InvalidOperationException("systemPrompt.md not found"));
        string promptText = await ReadAndReplacePromp(targetLanguage, targetFramework, otherRequirements, promptReader);

        //按模块处理
        foreach (var module in codeFileModules)
        {
            Console.WriteLine($"模块: {module.module}");
            Console.WriteLine($"文件夹路径: {module.directory}");
            Console.WriteLine($"目标文件夹路径: {module.targetDirectory}");
            allNoticeWriter.WriteLine($"模块: {module.module}");
            allNoticeWriter.WriteLine($"文件夹路径: {module.directory}");
            allNoticeWriter.WriteLine($"目标文件夹路径: {module.targetDirectory}");
            var moduleMessage = ChatMessage.CreateSystemMessage(promptText);

            ChatMessage[] moduleMessages = [moduleMessage, assessmentChatMessage, userChatMessage, assistantMessage];
            string moduleDirectory = Path.Combine(unzipPath, Path.GetFileNameWithoutExtension(fileFullName), module.directory);
            //文件夹不存在则跳过
            if (!Directory.Exists(moduleDirectory))
            {
                Console.WriteLine($"文件夹不存在: {moduleDirectory}");
                allNoticeWriter.WriteLine($"文件夹不存在: {moduleDirectory}");
                continue;
            }

            await ProcessFolder(client, convertFilePath, allNoticeWriter, options, module, moduleMessages, moduleDirectory);
        }

        Console.WriteLine($"ChatConvertFinished");
        Console.ReadLine();
    }

    private static async Task ProcessFolder(ChatClient client, string convertFilePath, StreamWriter allNoticeWriter, ChatCompletionOptions options, CodeFileModule module, ChatMessage[] moduleMessages, string moduleDirectory)
    {
        //先处理子文件夹
        var subDirectories = Directory.GetDirectories(moduleDirectory, "*", SearchOption.TopDirectoryOnly);
        foreach (var subDirectory in subDirectories)
        {
            Console.WriteLine($"继续处理子文件夹: {subDirectory}");
            await ProcessFolder(client, convertFilePath, allNoticeWriter, options, module, moduleMessages, subDirectory);
        }
    
        var directortyFiles = Directory.GetFiles(moduleDirectory, "*", SearchOption.TopDirectoryOnly);

        foreach (var file in directortyFiles)
        {
            Console.WriteLine($"文件路径: {file}");
            allNoticeWriter.WriteLine($"文件路径: {file}");
            var fileContent = File.ReadAllText(file);
            var fileMessage = ChatMessage.CreateUserMessage($"当前处理文件夹：{module.directory}，以下为文件{file}的内容：" + Environment.NewLine + fileContent);
            ChatMessage[] fileMessages = [.. moduleMessages, fileMessage];

            //向LLM发送文件转换消息
            string path = $"{Path.GetFileNameWithoutExtension(file)}{DateTime.Now.Ticks}.md";
            Console.WriteLine($"[写入转换情况]: {path}");

            string codeConvertContent = "";
            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "代码转换:" + file);
            await foreach (StreamingChatCompletionUpdate delta in client.CompleteChatStreamingAsync(fileMessages, options))
            {
                if (delta.ContentUpdate.Count == 0) continue;
                var content = delta.ContentUpdate[0].Text;
                codeConvertContent += content;
                Console.Write(content);
            }

            Console.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + $"[完整代码转换结果]: {codeConvertContent}");
            allNoticeWriter.WriteLine($"[完整代码转换结果]: {codeConvertContent}");
            var markdownResult = Markdown.Parse(codeConvertContent);

            // 使用正则表达式去除markdown的json标记及前后多余内容
            string fileJson = Regex.Replace(codeConvertContent, @"```json\s*([\s\S]*?)\s*```", "$1").Trim();
            var conversionResult = JsonConvert.DeserializeObject<ConversionResult>(fileJson);

            if (conversionResult.Status == "fail")
            {
                allNoticeWriter.WriteLine($"[文件转换失败]: {file}");
                // 需要人工处理
                continue;
            }
            else if (conversionResult.Status == "skip")
            {
                allNoticeWriter.WriteLine($"[文件跳过]: {file}");
                // 跳过
                continue;
            }
            else
            {
                // 成功
                // 将转换后的代码文件保存到目标技术栈内
                foreach (var codeFile in conversionResult.Files)
                {
                    var targetFilePath = Path.Combine(convertFilePath, module.targetDirectory, codeFile.FileName);
                    // 如果文件夹不存在，则创建文件夹
                    var targetDir = Path.GetDirectoryName(targetFilePath);
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }
                    Console.WriteLine($"[文件写入路径]: {targetFilePath}");
                    File.WriteAllText(targetFilePath, codeFile.CodeContent);
                }
            }

        }
    
    }

    private static async Task<string> ReadAndReplacePromp(string targetLanguage, string targetFramework, string otherRequirements, StreamReader promptReader)
    {
        string promptText = await promptReader.ReadToEndAsync();
        promptText = promptText.Replace("{{ target_language }}", targetLanguage).Replace("{{ target_framework }}", targetFramework);
        promptText = promptText.Replace("{{ other_requirements }}", otherRequirements);
        return promptText;
    }

    private static string ParseFolderStructure(string unzipPath)
    {
        //递归获取所有目录和文件
        var allDirectories = Directory.GetDirectories(unzipPath, "*", SearchOption.AllDirectories);
        var files = Directory.GetFiles(unzipPath, "*", SearchOption.AllDirectories);

        //构建项目文件结构字符串
        var fileStructure = new System.Text.StringBuilder();
        fileStructure.AppendLine("# 项目文件结构如下");

        //获取根目录名称
        //var rootDirName = new DirectoryInfo(unzipPath).Name;
        //fileStructure.AppendLine($"{rootDirName}/");

        //按目录处理
        foreach (var dir in allDirectories.OrderBy(d => d))
        {
            //计算相对路径
            var relativePath = dir.Replace(unzipPath, "").TrimStart(Path.DirectorySeparatorChar);
            var pathParts = relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            var depth = pathParts.Length - 1;
            var indent = new string(' ', depth * 4);

            //添加目录
            fileStructure.AppendLine($"{indent}├── {Path.GetFileName(dir)}/");

            //获取当前目录下的文件
            var dirFiles = files.Where(f => Path.GetDirectoryName(f) == dir)
                               .Where(f => !f.EndsWith(".sql") && !f.EndsWith(".gitkeep"))
                               .OrderBy(f => f);

            //添加文件
            foreach (var file in dirFiles)
            {
                var fileIndent = new string(' ', (depth + 1) * 4);
                fileStructure.AppendLine($"{fileIndent}├── {Path.GetFileName(file)}");
            }
        }

        var fileStructureString = fileStructure.ToString();
        Console.WriteLine(fileStructureString);
        return fileStructureString;
    }

    // 辅助方法：获取内联文本内容
    private static string GetInlineText(ContainerInline inline)
    {
        if (inline == null) return string.Empty;
        return string.Join("", inline.Descendants()
            .Where(x => x is LiteralInline)
            .Select(x => ((LiteralInline)x).Content.ToString()));
    }
}
