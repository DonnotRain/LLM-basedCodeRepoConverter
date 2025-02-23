using System.ClientModel;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;
using Newtonsoft.Json;
using LLMSmartConverter.Models;
using System.Text.RegularExpressions;
using System.IO.Compression;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace LLMSmartConverter;

//This is a code converter that converts code from one language to another.
public class CodeConverter
{
    public static async Task Analyse(string systemRequirement)
    {
        OpenAIClient api = new(new ApiKeyCredential("sk-dasafbxnbbbzwntu"), new OpenAIClientOptions()
        {
            Endpoint = new Uri("https://cloud.infini-ai.com/maas/v1"),
        });
        var client = api.GetChatClient("deepseek-v3");

        string targetLanguage = "C#";
        string targetFramework = "ASP.NET Core+EF Core+Redis";
        //其他要求：如代码规范、安全标准、类型映射规则、目标库、目标模式等
        string otherRequirements = "Json处理尽量使用System.Text.Json而不是Newtonsoft.Json";

        //从嵌入的资源：reasoningSystemPrompt.md 读取提示词模板 
        using StreamReader reader = new(typeof(CodeConverter).Assembly.GetManifestResourceStream("LLMSmartConverter.templates.reasoningSystemPrompt.md") ?? throw new InvalidOperationException("reasoningSystemPrompt.md not found"));
        string systemPromptText = await reader.ReadToEndAsync();
        // Console.WriteLine(systemPromptText);

        //替换{{ target_language }} {{ target_framework }}
        systemPromptText = systemPromptText.Replace("{{ target_language }}", targetLanguage).Replace("{{ target_framework }}", targetFramework);
        //替换{{ other_requirements }}
        systemPromptText = systemPromptText.Replace("{{ other_requirements }}", otherRequirements);

        var systemMessage = ChatMessage.CreateSystemMessage(systemPromptText);

        //从压缩包中读取文件结构     
        string fileFullName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "temp", "mldong-master.zip");
        //解压压缩包
        var unzipPath = Path.Combine(Path.GetDirectoryName(fileFullName), Path.GetFileNameWithoutExtension(fileFullName));
        if (Directory.Exists(unzipPath))
        {
            Directory.Delete(unzipPath, true);
        }
        ZipFile.ExtractToDirectory(fileFullName, unzipPath, true);

        //递归获取所有目录和文件
        var allDirectories = Directory.GetDirectories(unzipPath, "*", SearchOption.AllDirectories);
        var files = Directory.GetFiles(unzipPath, "*", SearchOption.AllDirectories);

        //构建项目文件结构字符串
        var fileStructure = new System.Text.StringBuilder();
        fileStructure.AppendLine("# 项目文件结构如下");

        //获取根目录名称
        var rootDirName = new DirectoryInfo(unzipPath).Name;
        fileStructure.AppendLine($"{rootDirName}/");

        //按目录处理
        foreach (var dir in allDirectories.OrderBy(d => d))
        {
            //计算相对路径
            var relativePath = dir.Replace(unzipPath, "").TrimStart(Path.DirectorySeparatorChar);
            var pathParts = relativePath.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            var depth = pathParts.Length;
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
        string structureString = "以下为原项目文件结构：" + Environment.NewLine + fileStructureString;
        structureString += Environment.NewLine;

        //读取readme.md文件内容 
        string readmeFilePath = Path.Combine(unzipPath, "readme.md");
        if (File.Exists(readmeFilePath))
        {
            structureString += Environment.NewLine;
            structureString += "以下为原项目的readme.md文件内容：" + Environment.NewLine + File.ReadAllText(readmeFilePath);
        }

        var userChatMessage = ChatMessage.CreateUserMessage(structureString);

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
            // Console.WriteLine($"[ASSISTANT]: {content}, FinishReason: {delta.FinishReason}, Usage: {delta.Usage}");
        }
        await writer.FlushAsync();
        Console.WriteLine($"[完整回复]: {allccontent}");

        string resultFilePath = $"readme{DateTime.Now.Ticks}.md";
        File.WriteAllText(resultFilePath, allccontent);

        // 使用正则表达式去除markdown的json标记及前后多余内容
        string cleanJson = Regex.Replace(allccontent, @"```json\s*([\s\S]*?)\s*```", "$1").Trim();
        var codeFileModules = JsonConvert.DeserializeObject<List<CodeFileModule>>(cleanJson);

        //从嵌入的资源：reasoningSystemPrompt.md 读取提示词模板 
        using StreamReader promptReader = new(typeof(CodeConverter).Assembly.GetManifestResourceStream("LLMSmartConverter.templates.systemPrompt.md") ?? throw new InvalidOperationException("systemPrompt.md not found"));
        string promptText = await promptReader.ReadToEndAsync();
        promptText = promptText.Replace("{{ target_language }}", targetLanguage).Replace("{{ target_framework }}", targetFramework);
        promptText = promptText.Replace("{{ other_requirements }}", otherRequirements);
        // Console.WriteLine(promptText);
        //转换后的文件保存路径 
        var convertFilePath = Path.Combine(Path.GetDirectoryName(fileFullName), Path.GetFileNameWithoutExtension(fileFullName) + "_converted");
        //确保文件夹存在
        if (!Directory.Exists(convertFilePath))
        {
            Directory.CreateDirectory(convertFilePath);
        }

        //按模块处理
        foreach (var module in codeFileModules)
        {
            Console.WriteLine($"模块: {module.module}");
            Console.WriteLine($"文件夹路径: {module.directory}");

            var moduleMessage = ChatMessage.CreateSystemMessage(promptText);

            //助手回复的消息
            var assistantMessage = ChatMessage.CreateAssistantMessage(allccontent);
            ChatMessage[] moduleMessages = [moduleMessage, userChatMessage, assistantMessage];
            var directortyFiles = Directory.GetFiles(Path.Combine(unzipPath, module.directory), "*", SearchOption.TopDirectoryOnly);
            //var directortyFiles = Directory.GetFiles(Path.Combine(Path.GetDirectoryName(fileFullName), module.directory), "*", SearchOption.TopDirectoryOnly);
            foreach (var file in directortyFiles)
            {
                Console.WriteLine($"文件路径: {file}");
                var fileContent = File.ReadAllText(file);
                var fileMessage = ChatMessage.CreateUserMessage($"当前处理文件夹：{module.directory}，以下为文件{file}的内容：" + Environment.NewLine + fileContent);
                ChatMessage[] fileMessages = [.. moduleMessages, fileMessage];

                //向LLM发送文件转换消息
                string path = $"{Path.GetFileNameWithoutExtension(file)}{DateTime.Now.Ticks}.md";
                Console.WriteLine($"[写入转换情况]: {path}");
                using StreamWriter codeWriter = new(path, false);
                string codeConvertContent = "";
                await foreach (StreamingChatCompletionUpdate delta in client.CompleteChatStreamingAsync(fileMessages, options))
                {
                    if (delta.ContentUpdate.Count == 0) continue;
                    var content = delta.ContentUpdate[0].Text;
                    await codeWriter.WriteAsync(content);
                    codeConvertContent += content;
                    // Console.WriteLine($"[ASSISTANT]: {content}, FinishReason: {delta.FinishReason}, Usage: {delta.Usage}");
                }
                await codeWriter.FlushAsync();

                Console.WriteLine($"[完整代码转换结果]: {codeConvertContent}");
                var document = Markdown.Parse(codeConvertContent);

                // 使用正则表达式去除markdown的json标记及前后多余内容
                string fileJson = Regex.Replace(codeConvertContent, @"```json\s*([\s\S]*?)\s*```", "$1").Trim();
                var conversionResult = JsonConvert.DeserializeObject<ConversionResult>(fileJson);

                if (conversionResult.Status == "fail")
                {
                    // 需要人工处理
                    continue;
                }
                else if (conversionResult.Status == "skip")
                {
                    // 跳过
                    continue;
                }
                else
                {
                    // 成功
                    // 将转换后的代码文件保存到目标技术栈内
                    var targetFilePath = Path.Combine(convertFilePath, conversionResult.FileRelativePath);
                    // 如果文件夹不存在，则创建文件夹
                    var targetDir = Path.GetDirectoryName(targetFilePath);
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }
                    Console.WriteLine($"[文件写入路径]: {targetFilePath}");
                    File.WriteAllText(targetFilePath, conversionResult.CodeContent);
                }

                //// 遍历所有 Markdown 节点
                //foreach (var node in document.Descendants())
                //{
                //    switch (node)
                //    {
                //        case HeadingBlock heading:
                //            var headingText = heading.Inline?.FirstOrDefault()?.ToString()?.Trim();
                //            switch (headingText)
                //            {
                //                case "转换结果":
                //                    conversionResult.Status = GetInlineText(heading.Inline!);
                //                    break;
                //                case "转换失败原因":
                //                    conversionResult.FailureReason = GetInlineText(heading.Inline!);
                //                    break;
                //                case "文件路径":
                //                    conversionResult.FilePath = GetInlineText(heading.Inline!);
                //                    break;
                //                case "注意事项":
                //                    conversionResult.Notes = [GetInlineText(heading.Inline!)];
                //                    break;
                //                default:
                //                    // ... 其他标题处理
                //                    break;
                //            }
                //            break;

                //        case FencedCodeBlock codeBlock:

                //            //解析出代码内容里的文件名
                //            var fileName = Path.GetFileName(file);

                //            if (codeBlock.Info?.Contains("csharp") == true || codeBlock.Info?.Contains("json") == true)
                //            {
                //                conversionResult.CodeContent = string.Join(Environment.NewLine,
                //                    codeBlock.Lines.Lines.Select(x => x.ToString()));
                //            }
                //            break;

                //        case ListBlock listBlock:
                //            var notes = new List<string>();
                //            foreach (var item in listBlock.Descendants().OfType<ParagraphBlock>())
                //            {
                //                notes.Add(GetInlineText(item.Inline!));
                //            }
                //            conversionResult.Notes = notes;
                //            break;
                //    }
                //}

                ////注意事项统一保存到同一个Markdown文件内，按原始文件名做标题
                //var notesFilePath = Path.Combine(convertFilePath, $"{Path.GetFileNameWithoutExtension(file)}.md");

                //Console.WriteLine($"[写入注意事项]: {notesFilePath}");
                //File.WriteAllText(notesFilePath, conversionResult.Notes.ToString());

                ////代码保存到对应的目录结构里(根据原始代码目录结构)
                //var codeFilePath = Path.Combine(convertFilePath, module.directory, Path.GetFileName(file));
                //Console.WriteLine($"[写入代码]: {codeFilePath}");
                //File.WriteAllText(codeFilePath, conversionResult.CodeContent);
            }
        }

        //// 使用正则表达式去除markdown的json标记及前后多余内容
        //string cleanJson = Regex.Replace(allccontent, @"```json\s*([\s\S]*?)\s*```", "$1").Trim();
        //var json = JsonConvert.DeserializeObject<List<SystemRequirementModule>>(cleanJson);
        ////写入到JSON文件
        //string jsonFilePath = $"readme{DateTime.Now.Ticks}.json";
        //File.WriteAllText(jsonFilePath, JsonConvert.SerializeObject(json, Formatting.Indented));
        Console.WriteLine($"ChatFinished");
        Console.ReadLine();
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
