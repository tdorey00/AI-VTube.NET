namespace AI_Vtube_dotNET.Livestream.Models;

/// <summary>
/// A Record for holding auth tokens and their respective refresh token.
/// </summary>
/// <param name="token">The authorization token</param>
/// <param name="refreshToken">The refresh token if applicable</param>
internal record AuthToken(string token, string refreshToken);
