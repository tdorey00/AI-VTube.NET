using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Client.Exceptions;


namespace AI_Vtube_dotNET.Livestream.Twitch;

internal sealed class TwitchAuth
{
    private string RedirectUrl;
    private readonly string State;
    private readonly TwitchAPI twitchAPI;
    private List<AuthScopes> scopes;
    private HttpListener listener;
    private readonly string ClientSecret;
    public TwitchAuth(string redirectUrl, string clientID, string clientSecret)
    {

        twitchAPI = new TwitchAPI();
        twitchAPI.Settings.ClientId = clientID;

        RedirectUrl = redirectUrl;
        ClientSecret = clientSecret;
        State = Guid.NewGuid().ToString();

        //TODO: Investigate what scopes we actually need.
        scopes = [AuthScopes.Any];

        listener = new HttpListener();
        listener.Prefixes.Add(RedirectUrl);
    }

    public string GetAuthorizationToken()
    {
        string authUrl = twitchAPI.Auth.GetAuthorizationCodeUrl(RedirectUrl, scopes, false, State);
        Task<string?> task = Task.Run(Listen);
        OpenUrl(authUrl);
        string? code = task.GetAwaiter().GetResult();

        if(code == null)
        {
            throw new BadStateException("No Twitch OAuth Code Found.");
        }

        //TODO: Once token works make this better.
        //OpenUrl(GetAuthTokenUrl());
        //return task.GetAwaiter().GetResult();
        return twitchAPI.Auth.GetAccessTokenFromCodeAsync(code, ClientSecret, RedirectUrl).GetAwaiter().GetResult().AccessToken;
    }

    private async Task<string?> Listen()
    {
        listener.Start();
        string? result = await onRequest();
        listener.Stop();
        return result;
    }

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

    //private string GetAuthTokenUrl()
    //{
    //    string scopesStr = null;
    //    foreach (var scope in scopes)
    //        if (scopesStr == null)
    //            scopesStr = TwitchLib.Api.Core.Common.Helpers.AuthScopesToString(scope);
    //        else
    //            scopesStr += $"+{TwitchLib.Api.Core.Common.Helpers.AuthScopesToString(scope)}";

    //    return "https://id.twitch.tv/oauth2/authorize?" +
    //            $"client_id={ClientID}&" +
    //            $"redirect_uri={System.Web.HttpUtility.UrlEncode(RedirectUrl)}&" +
    //            "response_type=token&" +
    //            $"scope={scopesStr}&" +
    //            $"state={State}&" +
    //            $"force_verify=false";
    //}

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
