using AI_Vtube_dotNET.Livestream;
using AI_Vtube_dotNET.Livestream.Impl;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AI_Vtube_dotNET.Core;

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
        //TODO WHEN TWITCH AUTH WORKS, UNCOMMENT AND REMOVE PLAT GARBAGE
        var plat = _livestreamPlatform as TwitchManager;
        plat.SetupConnectionCredentials();
        //_livestreamPlatform.InitClient();
        //_livestreamPlatform.RunClient();

        //while (true)
        //{
        //    Thread.Sleep(10000);
        //    var messages = _livestreamPlatform.GetChatMessages();
        //    _logger.LogInformation("Got {cnt} chat messages!", messages.Count);
        //    messages.ForEach(message => _logger.LogInformation("UserName: {user} Message: {message}", message.UserName, message.Message));
        //}
    }
}
