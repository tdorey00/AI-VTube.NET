﻿using AI_Vtube_dotNET.Livestream;
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
        _livestreamPlatform.InitClient();
        _livestreamPlatform.RunClient();
    }
}
