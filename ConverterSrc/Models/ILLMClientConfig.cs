using OpenAI;

namespace LLMSmartConverter.Models;

/// <summary>
/// LLM客户端配置接口
/// </summary>
public interface ILLMClientConfig
{
    string ApiKey { get; }
    string Endpoint { get; }
    string ModelName { get; }
    string ReseaningModelName { get; }
    void ChangeModel(bool reasoning = false);
 
} 