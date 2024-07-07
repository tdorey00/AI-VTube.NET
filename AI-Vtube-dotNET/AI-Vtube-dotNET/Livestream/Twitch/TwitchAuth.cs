using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;

namespace AI_Vtube_dotNET.Livestream.Twitch;

internal class TwitchAuth
{
    private string RedirectUrl;
    private readonly string State;
    private readonly TwitchAPI twitchAPI;
    private List<AuthScopes> scopes;

    public TwitchAuth(string clientID, string redirectUrl)
    {
        twitchAPI = new TwitchAPI();
        twitchAPI.Settings.ClientId = clientID;

        RedirectUrl = redirectUrl;
        State = Guid.NewGuid().ToString();

        //TODO: Investigate what scopes we actually need.
        scopes = [AuthScopes.Any];
    }

    public string GetAuthorizationCreds()
    {
        string authCodeUrl = twitchAPI.Auth.GetAuthorizationCodeUrl(RedirectUrl, scopes, false, State);
        StartLocalWebServer();
        OpenUrl(authCodeUrl);
        return "";
    }

    private void StartLocalWebServer()
    {
        HttpListener httpListener = new HttpListener();
        httpListener.Prefixes.Add(RedirectUrl);
        httpListener.Start();
        httpListener.BeginGetContext(new AsyncCallback(IncomingHttpRequest), httpListener);
    }


    /// <summary>
    /// Handles the incoming HTTP request
    /// </summary>
    /// <param name="result"></param>
    private void IncomingHttpRequest(IAsyncResult result)
    {
        HttpListener httpListener;
        HttpListenerContext httpContext;
        HttpListenerRequest httpRequest;
        HttpListenerResponse httpResponse;
        string responseString;

        // get back the reference to our http listener
        httpListener = (HttpListener)result.AsyncState!;

        // fetch the context object
        httpContext = httpListener.EndGetContext(result);
        // if we'd like the HTTP listener to accept more incoming requests, we'd just restart the "get context" here:
        httpListener.BeginGetContext(new AsyncCallback(IncomingAuth), httpListener);

        // the context object has the request object for us, that holds details about the incoming request
        httpRequest = httpContext.Request;


        // build a response to send JS back to the browser for OAUTH Relay
        httpResponse = httpContext.Response;
        responseString = "<html><body><b>Login Complete</b></body></html>";


        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);

        // send the output to the client browser
        httpResponse.ContentLength64 = buffer.Length;
        Stream output = httpResponse.OutputStream;
        output.Write(buffer, 0, buffer.Length);
        output.Close();

    }

    private void IncomingAuth(IAsyncResult ar)
    {
        //mostly the same as IncomingHttpRequest
        HttpListener httpListener;
        HttpListenerContext httpContext;
        HttpListenerRequest httpRequest;

        httpListener = (HttpListener)ar.AsyncState!;
        httpContext = httpListener.EndGetContext(ar);
        httpListener.BeginGetContext(new AsyncCallback(IncomingAuth), httpListener);

        httpRequest = httpContext.Request;

        //this time we take an input stream from the request to recieve the url
        string url;
        using (var reader = new StreamReader(httpRequest.InputStream,
                                             httpRequest.ContentEncoding))
        {
            url = reader.ReadToEnd();
        }

        Console.WriteLine(url);

        httpListener.Stop();
    }
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
