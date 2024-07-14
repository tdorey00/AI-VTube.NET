namespace AI_Vtube_dotNET.Livestream.Models;

/// <summary>
/// Record representing a message from a livestream
/// </summary>
/// <param name="UserName">The user who sent the message</param>
/// <param name="Message">The contents of the message</param>
public record LiveStreamMessage(string UserName, string Message);
