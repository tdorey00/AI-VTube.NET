using Microsoft.Extensions.Logging;
using OpenAI.Chat;

using System;
using System.Threading.Tasks;

namespace AI_Vtube_dotNET.Core;

public class Runtime
{
    private readonly ILogger _logger;
    public Runtime(ILogger<Runtime> logger) 
    { 
        _logger = logger;
    }

    public async Task RunAsync()
    {
        _logger.LogInformation("Hi");
        await SimpleChatAsync();
    }

    public async Task SimpleChatAsync()
    {
        ChatClient client = new(model: "gpt-3.5-turbo", "REPLACE ME"); // TODO: Move api key to more secure location

        List<UserChatMessage> messages = new List<UserChatMessage>();
        UserChatMessage message = new UserChatMessage("List all prime numbers between 1 and 100");
        messages.Add(message);

        ChatCompletion completion = await client.CompleteChatAsync(messages);
        _logger.LogInformation(completion.Content[0].Text);
    }
}
