namespace AI_Vtube_dotNET.Livestream;

/// <summary>
/// An interface for defining common functionality of different Live stream platforms
/// </summary>
internal interface ILivestreamPlatform
{
    /// <summary>
    /// Initialize the client and preform any necessary setup
    /// </summary>
    public void InitClient();

    /// <summary>
    /// Start the Client thats connected to the livestream
    /// </summary>
    public void RunClient();

    /// <summary>
    /// Grabs a list of chat messages from the live stream platform.
    /// </summary>
    /// <returns>A list of strings containing chat messages from the live stream platform</returns>
    public List<string> GetChatMessages();
}
