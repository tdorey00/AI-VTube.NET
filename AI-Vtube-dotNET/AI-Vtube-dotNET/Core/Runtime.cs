using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;

namespace AI_Vtube_dotNET.Core;

public class Runtime
{
    private readonly ILogger _logger;
    private readonly IConfiguration _configuration;
    public Runtime(ILogger<Runtime> logger, IConfiguration config) 
    { 
        _logger = logger;
        _configuration = config;
    }

    public async Task RunAsync()
    {
        await SimpleChatAsync();
    }

    public async Task SimpleChatAsync()
    {

        //TODO: This is garbage... figure out how to be smart. IT IS THREAD SAFE THO
        ChatClient client = new(model: "gpt-3.5-turbo", credential: _configuration.GetValue<string>("OpenAI_API_KEY")!); // TODO: Move api key to more secure location
        SemaphoreQueue queue = new(1);
        string[] prompts = ["What color is the sky?", "Whats the best programming language?", "Why is it so hard to multithread programs?", "When did shakespeare die?", "Is your mom gay?"];

        List<Task> taskList = [];

        taskList.Add(Task.Run(async () => {
            _logger.LogInformation("Prompt1 Start...");
            List<UserChatMessage> messages = new List<UserChatMessage>();
            UserChatMessage message = new UserChatMessage(prompts[0]);
            messages.Add(message);
            _logger.LogInformation("Prompt1 Request...");
            ChatCompletion completion = await client.CompleteChatAsync(messages);
            _logger.LogInformation("Prompt1 waiting for availability...");
            queue.Wait();
            _logger.LogInformation("Prompt1 reading...");
            _logger.LogInformation(completion.Content[0].Text);
            queue.Release();
            _logger.LogInformation("Prompt1 release...");
        }));

        taskList.Add(Task.Run(async () => {
            _logger.LogInformation("Prompt2 Start...");
            List<UserChatMessage> messages = new List<UserChatMessage>();
            UserChatMessage message = new UserChatMessage(prompts[1]);
            messages.Add(message);
            _logger.LogInformation("Prompt2 Request...");
            ChatCompletion completion = await client.CompleteChatAsync(messages);
            _logger.LogInformation("Prompt2 waiting for availability...");
            queue.Wait();
            _logger.LogInformation("Prompt2 reading...");
            _logger.LogInformation(completion.Content[0].Text);
            queue.Release();
            _logger.LogInformation("Prompt2 release...");
        }));

        taskList.Add(Task.Run(async () => {
            _logger.LogInformation("Prompt3 Start...");
            List<UserChatMessage> messages = new List<UserChatMessage>();
            UserChatMessage message = new UserChatMessage(prompts[2]);
            messages.Add(message);
            _logger.LogInformation("Prompt3 Request...");
            ChatCompletion completion = await client.CompleteChatAsync(messages);
            _logger.LogInformation("Prompt3 waiting for availability...");
            queue.Wait();
            _logger.LogInformation("Prompt3 reading...");
            _logger.LogInformation(completion.Content[0].Text);
            queue.Release();
            _logger.LogInformation("Prompt3 release...");
        }));

        taskList.Add(Task.Run(async () => {
            _logger.LogInformation("Prompt4 Start...");
            List<UserChatMessage> messages = new List<UserChatMessage>();
            UserChatMessage message = new UserChatMessage(prompts[3]);
            messages.Add(message);
            _logger.LogInformation("Prompt4 Request...");
            ChatCompletion completion = await client.CompleteChatAsync(messages);
            _logger.LogInformation("Prompt4 waiting for availability...");
            queue.Wait();
            _logger.LogInformation("Prompt4 reading...");
            _logger.LogInformation(completion.Content[0].Text);
            queue.Release();
            _logger.LogInformation("Prompt4 release...");
        }));

        taskList.Add(Task.Run(async () => {
            _logger.LogInformation("Prompt5 Start...");
            List<UserChatMessage> messages = new List<UserChatMessage>();
            UserChatMessage message = new UserChatMessage(prompts[4]);
            messages.Add(message);
            _logger.LogInformation("Prompt5 Request...");
            ChatCompletion completion = await client.CompleteChatAsync(messages);
            _logger.LogInformation("Prompt5 waiting for availability...");
            queue.Wait();
            _logger.LogInformation("Prompt5 reading...");
            _logger.LogInformation(completion.Content[0].Text);
            queue.Release();
            _logger.LogInformation("Prompt5 release...");
        }));
        Task.WaitAll(taskList.ToArray());

    }
}
