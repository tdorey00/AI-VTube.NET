using AI_Vtube_dotNET.Core;
using Microsoft.Extensions.Logging;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Interfaces;
using TwitchLib.Communication.Models;

namespace AI_Vtube_dotNET.Livestream.Impl;

internal sealed class TwitchManager : ILivestreamPlatform
{
    private readonly TwitchClient _client;
    private readonly ILogger<TwitchManager> _logger;

    public TwitchManager(ILogger<TwitchManager> logger) 
    {
        _logger = logger;

        ClientOptions clientOptions = new ClientOptions
        {
            MessagesAllowedInPeriod = 750, // TODO: See what this actually does, see how we can turn these knobs
            ThrottlingPeriod = TimeSpan.FromSeconds(30)
        };

        WebSocketClient customClient = new WebSocketClient(clientOptions);
        _client = new TwitchClient(customClient);
    }

    public void InitClient()
    {
        // TODO: Request OAuth token from twitch
        ConnectionCredentials credentials = new ConnectionCredentials("Twitch Channel Name", "OAuth Token");
        _client.Initialize(credentials, "Twitch Channel Name");

        // Bind client events
        _client.OnMessageReceived += Client_OnMessageReceived;
        _client.OnConnected += Client_OnConnected;
        _client.OnConnectionError += _client_OnConnectionError;
    }

    private void _client_OnConnectionError(object? sender, OnConnectionErrorArgs e)
    {
        _logger.LogError("Connection Failed!");
    }

    private void Client_OnConnected(object? sender, OnConnectedArgs e)
    {
        _logger.LogInformation("Connected to Twitch!");
    }

    public void RunClient()
    {
        _client.Connect();
    }

    private void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        // PROCESS MESSAGES HERE
        // We consume messages here, assuming we get MessagesAllowedInPeriod messages every ThrottlingPeriod seconds.

        _logger.LogInformation(e.ChatMessage.Message.ToString());

        // Should have "processing queue(s)" separate from consumption that has a much stricter limitation on the number
        // number of messages received.
    }
}
