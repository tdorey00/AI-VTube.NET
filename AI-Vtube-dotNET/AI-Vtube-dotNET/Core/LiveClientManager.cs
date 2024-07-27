using AI_Vtube_dotNET.Livestream;
using AI_Vtube_dotNET.Livestream.Models;
using AI_Vtube_dotNET.LLM;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AI_Vtube_dotNET.Core;
//TODO: Refactor this later when we're in a more mature state
/// <summary>
/// Manager for <see cref="ILivestreamPlatform"/> to interact with the runtime
/// </summary>
internal sealed class LiveClientManager
{
    private readonly ILogger<LiveClientManager> _logger;
    private readonly IConfiguration _configuration;
    private readonly ILivestreamPlatform _livestreamPlatform;
    private readonly ILLMBase _llm;

    public LiveClientManager(ILogger<LiveClientManager> logger, IConfiguration configuration, ILivestreamPlatform livestreamPlatform, ILLMBase llm)
    {
        _logger = logger;
        _configuration = configuration;
        _livestreamPlatform = livestreamPlatform;
        _llm = llm;
    }

    public void Init()
    {
        _llm.InitLLM();
        _livestreamPlatform.InitClient();
        _livestreamPlatform.RunClient();

        while (true)
        {
            Thread.Sleep(10000);
            var messages = _livestreamPlatform.GetChatMessages();
            _logger.LogInformation("Got {cnt} chat messages!", messages.Count);
            if (messages.Count > 0)
            {
                SendMessageToLLM(messages.Last());
                _logger.LogInformation("Starting LLM processing");
                _logger.LogInformation("Prompt: {words} Response: {moreWords}", messages.Last(), GetRawLLMResponse());
            }
        }
    }

    public string ProcessChatMessage()
    {
        return "";
    }

    public void SendMessageToLLM(LiveStreamMessage message)
    {
        _llm.RecievePrompt(message.Message);
    }

    public string GetRawLLMResponse()
    {
       return _llm.GetRawResponse();
    }
}
