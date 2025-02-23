namespace LLMSmartConverter.Models;

/// <summary>
/// DeepSeek官方配置
/// </summary>
public class DeepSeekConfig : ILLMClientConfig
{
    public string ApiKey { get; init; }
    public string Endpoint => "https://api.deepseek.com/v1";
    public string ModelName => "deepseek-coder";

    public string ReseaningModelName => throw new NotImplementedException();

    public DeepSeekConfig(string apiKey)
    {
        ApiKey = apiKey;
    }

    public void ChangeModel(bool reasoning = false)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 火山引擎
/// </summary>
public class VolcengineConfig : ILLMClientConfig
{
    public string ApiKey { get; init; }
    public string Endpoint => "https://ark.cn-beijing.volces.com/api/v3";
    public string ModelName => "deepseek-v3-241226";

    public VolcengineConfig(string apiKey)
    {
        ApiKey = apiKey;
    }

    public string ReseaningModelName => "deepseek-r1-250120";

    public void ChangeModel(bool reasoning = false)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Infini-AI配置
/// </summary>
public class InfiniAIConfig : ILLMClientConfig
{
    public string ApiKey { get; init; }
    public string Endpoint => "https://cloud.infini-ai.com/maas/v1";
    public string ModelName => "deepseek-v3";

    public InfiniAIConfig(string apiKey)
    {
        ApiKey = apiKey;
    }
    public string ReseaningModelName => throw new NotImplementedException();

    public void ChangeModel(bool reasoning = false)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// 腾讯LKEAP配置
/// </summary>
public class TencentLKEAPConfig : ILLMClientConfig
{
    public string ApiKey { get; init; }
    public string Endpoint => "https://api.lkeap.cloud.tencent.com/v1";
    public string ModelName => "deepseek-v3";

    public TencentLKEAPConfig(string apiKey)
    {
        ApiKey = apiKey;
    }

    public string ReseaningModelName => "deepseek-r1";

    public void ChangeModel(bool reasoning = false)
    {
        throw new NotImplementedException();
    }
}