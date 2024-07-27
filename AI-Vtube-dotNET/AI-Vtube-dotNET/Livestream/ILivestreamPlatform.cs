using AI_Vtube_dotNET.Livestream.Models;

namespace AI_Vtube_dotNET.Livestream;

/// <summary>
/// An interface for defining common functionality of different Live stream platforms
/// </summary>
public interface ILivestreamPlatform
{
    /// <summary>
    /// Initialize the client and preform any necessary setup
    /// </summary>
    void InitClient();

    /// <summary>
    /// Start the Client thats connected to the livestream
    /// </summary>
    void RunClient();

    /// <summary>
    /// Grabs a list of chat messages from the live stream platform.
    /// </summary>
    /// <returns>A list of strings containing chat messages from the live stream platform</returns>
    List<LiveStreamMessage> GetChatMessages();

    /// <summary>
    /// Get the current <see cref="ConnectionState"/> of the connection to the livestream platform.
    /// </summary>
    /// <returns>The current <see cref="ConnectionState"/></returns>
    ConnectionState GetConnectionState();

    /// <summary>
    /// Enum representing different connection states
    /// </summary>
    public enum ConnectionState //TODO: If we get too many enums, move enums to one common location.
    {
        Connected,
        Connecting,
        Reconnecting,
        Disconnected,
        Failed
    }
}
