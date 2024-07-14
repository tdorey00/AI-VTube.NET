using AI_Vtube_dotNET.Livestream;
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

    public LiveClientManager(ILogger<LiveClientManager> logger, IConfiguration configuration, ILivestreamPlatform livestreamPlatform)
    {
        _logger = logger;
        _configuration = configuration;
        _livestreamPlatform = livestreamPlatform;
    }

    public void Init()
    {
        _livestreamPlatform.InitClient();
        _livestreamPlatform.RunClient();

        while (true)
        {
            Thread.Sleep(10000);
            var messages = _livestreamPlatform.GetChatMessages();
            _logger.LogInformation("Got {cnt} chat messages!", messages.Count);
            messages.ForEach(message => _logger.LogInformation("UserName: {user} Message: {message}", message.UserName, message.Message));
        }
    }
}
