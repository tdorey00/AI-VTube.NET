using AI_Vtube_dotNET.Core;
using AI_Vtube_dotNET.Core.Queues;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Interfaces;
using TwitchLib.Communication.Models;

namespace AI_Vtube_dotNET.Livestream.Impl;

/// <summary>
/// A Manager for <see cref="TwitchClient"/> used to interact with the Twitch API
/// </summary>
internal sealed class TwitchManager : ILivestreamPlatform
{
    private readonly TwitchClient _client;
    private readonly ILogger<TwitchManager> _logger;
    private readonly IConfiguration _configuration;
    private readonly BatchQueue<string> _messageQueue;
    private const int MAX_CONNECTION_ATTEMPTS = 5;

    private int connectionAttempts = 0;

    public TwitchManager(ILogger<TwitchManager> logger, IConfiguration configuration) 
    {
        _logger = logger;
        _configuration = configuration;

        _messageQueue = new BatchQueue<string>(10, 100);

        ClientOptions clientOptions = new ClientOptions
        {
            MessagesAllowedInPeriod = 750, // TODO: See what this actually does, see how we can turn these knobs
            ThrottlingPeriod = TimeSpan.FromSeconds(30)
        };

        WebSocketClient customClient = new WebSocketClient(clientOptions);
        _client = new TwitchClient(customClient);
    }

    /// <inheritdoc cref="ILivestreamPlatform.InitClient"/>
    /// <exception cref="InvalidDataException">Thrown when bad data is found in configuration</exception>
    public void InitClient()
    {
        string? channelName = _configuration.GetValue<string>("Twitch_Channel_Name");
        if (channelName == null)
        {
            throw new InvalidDataException("Unable to find Twitch Channel name in configuration");
        }

        // TODO: Request OAuth token from twitch automagically
        ConnectionCredentials credentials = new ConnectionCredentials(channelName, "OAuth Token");
        _client.Initialize(credentials, channelName);

        // Bind client events
        _client.OnMessageReceived += Client_OnMessageReceived;
        _client.OnConnected += Client_OnConnected;
        _client.OnConnectionError += _client_OnConnectionError;
        //TODO: Consider if OnDisconnected is needed for some kind of state management (If disconnected maybe we need to expose some kind of state value for the LiveClientManager to be able to reference).
    }

    /// <inheritdoc cref="ILivestreamPlatform.RunClient"/>
    public void RunClient()
    {
        _client.Connect();
    }

    ///<inheritdoc cref="ILivestreamPlatform.GetChatMessages"/>
    public List<string> GetChatMessages()
    {
        return _messageQueue.GetNextBatch();
    }

#region EVENTS

    /// <summary>
    /// Called when client is sucessfully connected, resets connection attempts
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e">Connection Info</param>
    private void Client_OnConnected(object? sender, OnConnectedArgs e)
    {
        _logger.LogInformation("Connected to Twitch!");
        connectionAttempts = 0;
    }

    /// <summary>
    /// Called when a connection error occurs, will attempt to reconnect after a delay up until <see cref="MAX_CONNECTION_ATTEMPTS"/>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e">Connection Error Info</param>
    private void _client_OnConnectionError(object? sender, OnConnectionErrorArgs e)
    {
        _logger.LogError("Twitch Connection Failed: {Message}", e.Error.Message);
        //In the event of failure, we have some retry logic
        if (connectionAttempts < MAX_CONNECTION_ATTEMPTS)
        {
            Thread.Sleep(connectionAttempts * 1000);
            RunClient(); //Retry Connection
            connectionAttempts++;
        }
    }

    private void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        // PROCESS MESSAGES HERE
        // We consume messages here, assuming we get MessagesAllowedInPeriod messages every ThrottlingPeriod seconds.

        //TODO: Do something with the batch queue here, the twitch manager should maintain a batch queue of messages which the LiveClientManager can read from when it needs to
        _logger.LogInformation(e.ChatMessage.Message.ToString());

        _messageQueue.Add(e.ChatMessage.Message, true);

        // Should have "processing queue(s)" separate from consumption that has a much stricter limitation on the number
        // number of messages received.
    }

#endregion EVENTS

}
