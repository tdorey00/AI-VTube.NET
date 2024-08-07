﻿using AI_Vtube_dotNET.Core.Queues;
using AI_Vtube_dotNET.Livestream.Models;
using AI_Vtube_dotNET.Livestream.Twitch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Exceptions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;
using static AI_Vtube_dotNET.Livestream.ILivestreamPlatform;

namespace AI_Vtube_dotNET.Livestream.Impl;

/// <summary>
/// A Manager for <see cref="TwitchClient"/> used to interact with the Twitch API
/// </summary>
internal sealed class TwitchManager : ILivestreamPlatform
{
    private const int MAX_CONNECTION_ATTEMPTS = 5;
    private const int QUEUE_BATCH_SIZE = 10;
    private const int QUEUE_MAX_SIZE = 100;

    private readonly TwitchClient _client;
    private readonly ILogger<TwitchManager> _logger;
    private readonly IConfiguration _configuration;
    private readonly BatchQueue<LiveStreamMessage> _messageQueue;

    private int connectionAttempts = 0;
    private ConnectionState connectionState;

    public TwitchManager(ILogger<TwitchManager> logger, IConfiguration configuration) 
    {
        _logger = logger;
        _configuration = configuration;
        _messageQueue = new BatchQueue<LiveStreamMessage>(QUEUE_BATCH_SIZE, QUEUE_MAX_SIZE);
        
        ClientOptions clientOptions = new ClientOptions
        {
            MessagesAllowedInPeriod = 750, // TODO: See what this actually does, see how we can turn these knobs
            ThrottlingPeriod = TimeSpan.FromSeconds(30)
        };
        WebSocketClient customClient = new WebSocketClient(clientOptions);
        _client = new TwitchClient(customClient);

        connectionState = ConnectionState.Disconnected;
    }

    #region ILiveStreamPlatform Implementations

    /// <inheritdoc cref="ILivestreamPlatform.InitClient"/>
    /// <exception cref="InvalidDataException">Thrown when bad data is found in configuration</exception>
    public void InitClient()
    {
        string? channelName = _configuration.GetValue<string>("Twitch_Channel_Name");
        if (channelName == null)
        {
            connectionState = ConnectionState.Failed;
            throw new InvalidDataException("Unable to find Twitch Channel name in configuration");
        }

        _client.Initialize(SetupConnectionCredentials(channelName), channelName);

        BindClientEvents();
    }

    /// <inheritdoc cref="ILivestreamPlatform.RunClient"/>
    public void RunClient()
    {
        connectionState = ConnectionState.Connecting;
        _client.Connect();
    }

    ///<inheritdoc cref="ILivestreamPlatform.GetChatMessages"/>
    public List<LiveStreamMessage> GetChatMessages()
    {
        return _messageQueue.GetNextBatch();
    }

    ///<inheritdoc cref="ILivestreamPlatform.GetConnectionState"/>
    public ConnectionState GetConnectionState()
    {
        return connectionState;
    }

    #endregion ILiveStreamPlatform Implementations

    #region private members

    /// <summary>
    /// Generated the connection credentials for the <see cref="TwitchClient"/> using <see cref="TwitchAuth"/>
    /// </summary>
    /// <param name="channelName">The Twitch channel being connected</param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException">Thrown if data is missing from configuration</exception>
    /// <exception cref="BadStateException">Thrown if there is an issue with the token</exception>
    private ConnectionCredentials SetupConnectionCredentials(string channelName)
    {
        IConfigurationSection twitchOAuth = _configuration.GetRequiredSection("Twitch_OAuth");
        string? redirectURL = twitchOAuth.GetValue<string>("Redirect_URL");
        if (redirectURL == null)
        {
            connectionState = ConnectionState.Failed;
            throw new InvalidDataException("Unable to find Twitch Redirect URL in configuration");
        }

        string? clientId = twitchOAuth.GetValue<string>("Client_ID");
        if (clientId == null)
        {
            connectionState = ConnectionState.Failed;
            throw new InvalidDataException("Unable to find Twitch Client ID in configuration");
        }

        string? clientSecret = twitchOAuth.GetValue<string>("Client_Secret");
        if (clientSecret == null)
        {
            connectionState = ConnectionState.Failed;
            throw new InvalidDataException("Unable to find Twitch Client Secret in configuration");
        }

        TwitchAuth auth = new(redirectURL, clientId, clientSecret);
        AuthToken? authToken = auth.GetAuthorizationToken();

        if (authToken.token == null)
        {
            _logger.LogCritical("Unable to get authorization token, cannot connect to twitch.");
            throw new BadStateException("Unable to get authorization token.");
        }

        //TODO: Might not need channel name here, thats probably the bot username. 
        return new ConnectionCredentials(channelName, authToken.token);
    }

    /// <summary>
    /// Binds events to the <see cref="TwitchClient"/>
    /// </summary>
    private void BindClientEvents()
    {
        _client.OnMessageReceived += Client_OnMessageReceived;
        _client.OnConnected += Client_OnConnected;
        _client.OnConnectionError += Client_OnConnectionError;
        _client.OnError += Client_OnError;
        _client.OnIncorrectLogin += Client_OnIncorrectLogin;
        #if DEBUG
        //Debug only for this, it kind of spams the console
        _client.OnLog += Client_OnLog;
        #endif
        //TODO: Consider if OnDisconnected is needed for some kind of state management (We have connection state exposed so we might want to use that in this case).
    }

    #endregion private members

    #region EVENTS
    //TODO: In the event of an error that causes us to disconnect from twitch (Look through available events, we need to refresh the auth token using: https://github.com/TwitchLib/TwitchLib.Api/blob/816b6d46af4edb89f9f1f54d3344cd752a8f043f/TwitchLib.Api/Auth/Auth.cs#L25

    /// <summary>
    /// Called when client is sucessfully connected, resets connection attempts
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e">Connection Info</param>
    private void Client_OnConnected(object? sender, OnConnectedArgs e)
    {
        connectionState = ConnectionState.Connected;
        _logger.LogInformation("Connected to Twitch!");
        connectionAttempts = 0;
    }

    /// <summary>
    /// Called when a connection error occurs, will attempt to reconnect after a delay up until <see cref="MAX_CONNECTION_ATTEMPTS"/>
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e">Connection Error Info</param>
    private async void Client_OnConnectionError(object? sender, OnConnectionErrorArgs e)
    {
        _logger.LogError("Twitch Connection Failed: {Message}", e.Error.Message);
        //In the event of failure, we have some retry logic
        if (connectionAttempts < MAX_CONNECTION_ATTEMPTS)
        {
            connectionState = ConnectionState.Reconnecting;
            await Task.Delay(connectionAttempts * 100);
            //TODO: Consider refreshing and reinitializing the client on connection error
            RunClient(); //Retry Connection
            connectionAttempts++;
        }
        else
        {
            _logger.LogError("Could not connect to twitch after {Attmepts} attempts. Will not retry again.", MAX_CONNECTION_ATTEMPTS);
            connectionState = ConnectionState.Failed;
        }
    }

    private void Client_OnMessageReceived(object? sender, OnMessageReceivedArgs e)
    {
        // PROCESS MESSAGES HERE
        // We consume messages here, assuming we get MessagesAllowedInPeriod messages every ThrottlingPeriod seconds.

        _logger.LogInformation(e.ChatMessage.Message.ToString());

        _messageQueue.Add(new LiveStreamMessage(e.ChatMessage.Username, e.ChatMessage.Message), true);

        // Should have "processing queue(s)" separate from consumption that has a much stricter limitation on the number
        // number of messages received.
    }

    /// <summary>
    /// Logs anything we get from TwitchLib, only registered with a debug build
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e">Log Info</param>
    private void Client_OnLog(object? sender, OnLogArgs e)
    {
        _logger.LogInformation("TWITCHLIB DEBUG: {log}", e.Data);
    }

    /// <summary>
    /// Throws on Client error
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e">Error Event</param>
    private void Client_OnError(object? sender, OnErrorEventArgs e)
    {
        throw e.Exception;
    }

    /// <summary>
    /// Throwns on a bad login
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e">Incorrect Login Info</param>
    private void Client_OnIncorrectLogin(object? sender, OnIncorrectLoginArgs e)
    {
        //Not much else we can do here honestly
        throw e.Exception;
    }

    #endregion EVENTS

}
