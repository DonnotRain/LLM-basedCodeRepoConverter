using OpenAI;
using OpenAI.Chat;
using System.ClientModel;

namespace LLMSmartConverter.Models;

/// <summary>
/// LLM客户端工厂
/// </summary>
public class LLMClientFactory
{
    private readonly ILLMClientConfig _config;

    public LLMClientFactory(ILLMClientConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// 创建OpenAI客户端
    /// </summary>
    public OpenAIClient CreateClient()
    {
        var client = new OpenAIClient(
            new ApiKeyCredential(_config.ApiKey),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(_config.Endpoint)
            });

        return client;
    }

    /// <summary>
    /// 创建聊天客户端
    /// </summary>
    public ChatClient CreateChatClient(bool reasoning = false)
    {
        var client = CreateClient();
        return client.GetChatClient(reasoning ? _config.ReseaningModelName : _config.ModelName);
    }
}