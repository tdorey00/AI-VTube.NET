using AI_Vtube_dotNET.Livestream.Models;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Client.Exceptions;


namespace AI_Vtube_dotNET.Livestream.Twitch;

/// <summary>
/// A Class for handling the twitch OAuth Implicit grant flow using a <see cref="HttpListener"/> to recieve the Auth Code from the redirect URL
/// </summary>
internal sealed class TwitchAuth
{
    private string RedirectUrl;
    private readonly string State;
    private readonly TwitchAPI twitchAPI;
    private List<AuthScopes> scopes;
    private HttpListener listener;
    private readonly string ClientSecret;

    /// <summary>
    /// Instantiates the <see cref="TwitchAuth"/> class
    /// </summary>
    /// <param name="redirectUrl">The redirect URL for the Authorization Code</param>
    /// <param name="clientID">The client ID of the twitch app</param>
    /// <param name="clientSecret">The client secret of the twitch app</param>
    public TwitchAuth(string redirectUrl, string clientID, string clientSecret)
    {
        twitchAPI = new TwitchAPI();
        twitchAPI.Settings.ClientId = clientID;

        RedirectUrl = redirectUrl;
        ClientSecret = clientSecret;
        State = Guid.NewGuid().ToString();

        scopes = 
            [
            AuthScopes.Chat_Read,
            AuthScopes.Chat_Edit
            ];

        listener = new HttpListener();
        listener.Prefixes.Add(RedirectUrl);
    }

    /// <summary>
    /// Generates a Twitch API Token using the Implicit Grant Flow
    /// </summary>
    /// <returns>A <see cref="AuthToken"/> record containing the token information</returns>
    /// <exception cref="BadStateException">Thrown if the auth code used to generate a token is invalid</exception>
    public AuthToken GetAuthorizationToken()
    {
        string authUrl = twitchAPI.Auth.GetAuthorizationCodeUrl(RedirectUrl, scopes, false, State);
        Task<string?> task = Task.Run(Listen);
        OpenUrl(authUrl);
        string? code = task.GetAwaiter().GetResult();

        if(code == null)
        {
            throw new BadStateException("No Twitch OAuth Code Found.");
        }

        var authResponse = twitchAPI.Auth.GetAccessTokenFromCodeAsync(code, ClientSecret, RedirectUrl).GetAwaiter().GetResult();

        return new AuthToken(authResponse.AccessToken, authResponse.RefreshToken);
    }

    /// <summary>
    /// Starts the <see cref="HttpListener"/> and then begins the wait for the request
    /// </summary>
    /// <returns>The auth code if request was sucessful, false otherwise</returns>
    private async Task<string?> Listen()
    {
        listener.Start();
        string? result = await onRequest();
        listener.Stop();
        return result;
    }

    /// <summary>
    /// Waits for a request on the Redirect URL using the <see cref="HttpListener"/> then parses that checking the OAuth State and then for the Auth Code
    /// </summary>
    /// <returns>The auth code if present and the state is valid, null otherwise</returns>
    private async Task<string?> onRequest()
    {
        while (listener.IsListening)
        {
            var ctx = await listener.GetContextAsync();
            var req = ctx.Request;
            var resp = ctx.Response;

            using (var writer = new StreamWriter(resp.OutputStream))
            {
                if (req.QueryString.AllKeys.Any("state".Contains!))
                {

                    if (req.QueryString["state"] != State)
                    {
                        writer.WriteLine("Invalid state found, CSRF is assumed.");
                        writer.Flush();
                        return null;
                    }

                    if (req.QueryString.AllKeys.Any("code".Contains!))
                    {
                        writer.WriteLine("Authorization Started! Check your application!");
                        writer.Flush();
                        return req.QueryString["code"]!;
                    }
                    else
                    {
                        writer.WriteLine("No code found in query string!");
                        writer.Flush();
                        return null;
                    }
                }
                else
                {
                    writer.WriteLine("No state found in query string!");
                    writer.Flush();
                    return null;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Opens URL based on OS
    /// </summary>
    /// <param name="url">The Url to be opened</param>
    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(url);
        }
        catch
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                throw;
            }
        }
    }
}
