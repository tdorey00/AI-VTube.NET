using OpenAI.Chat;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using AI_Vtube_dotNET.LLM.Exceptions;

namespace AI_Vtube_dotNET.LLM.OpenAI;

/// <summary>
/// A Manager for interacting with OpenAI
/// </summary>
public sealed class OpenAIManager : ILLMBase
{
    private List<ChatMessage> ChatHistory = new();
    private ChatClient? client;

    private readonly ILogger<OpenAIManager> _logger;
    private readonly IConfiguration _configuration;

    public OpenAIManager(ILogger<OpenAIManager> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public void InitLLM()
    {
        string? openAiKey = _configuration.GetValue<string>("OpenAI_API_KEY"); // TODO: Move api key to more secure location
        if (openAiKey == null)
        {
            throw new InvalidDataException("Unable to find Twitch Redirect URL in configuration");
        }
        client = new(model: "gpt-3.5-turbo", credential: openAiKey);
    }

    public void RecievePrompt(string prompt)
    {
        ChatHistory.Add(new UserChatMessage(prompt));
    }

    public string GetRawResponse()
    {
        if(client is null)
        {
            throw new InvalidOperationException("Cannot call GetRawResponse if the LLM client has not been initialized! " +
                                                "Make sure InitLLM() has been called before trying to get a response.");
        }

        bool requiresAction;
        //TODO: Consider better exceptions... perhaps framework implements its own custom exceptions
        do
        {
            requiresAction = false;
            ChatCompletion chatCompletion = client.CompleteChat(ChatHistory);

            switch (chatCompletion.FinishReason)
            {
                case ChatFinishReason.Stop:
                    {
                        // Add the assistant message to the conversation history.
                        ChatHistory.Add(new AssistantChatMessage(chatCompletion));
                        break;
                    }

                case ChatFinishReason.ToolCalls:
                    //TODO: If we ever end up using these look at https://github.com/openai/openai-dotnet?tab=readme-ov-file#how-to-use-chat-completions-with-streaming
                    throw new LLMException("ToolCalls are not currently supported in our framework.", "OpenAI");

                case ChatFinishReason.Length:
                    throw new LLMException("Incomplete model output due to MaxTokens parameter or token limit exceeded.", "OpenAI");

                case ChatFinishReason.ContentFilter:
                    //Shouldn't occur because we should be filtering before adding responses - Framework users may encounter this though
                    throw new LLMException("Omitted content due to a content filter flag.", "OpenAI");

                case ChatFinishReason.FunctionCall:
                    throw new LLMException("Deprecated in favor of tool calls.", "OpenAI");

                default:
                    throw new LLMException(chatCompletion.FinishReason.ToString(), "OpenAI");
            }
        } while (requiresAction);

        //TODO: When this is running figure out if we actually need to do this garbage
        ChatMessage LastMessage = ChatHistory.Last();
        StringBuilder response = new();
        foreach (ChatMessageContentPart contentItem in LastMessage.Content)
        {
            if (!string.IsNullOrEmpty(contentItem.Text))
            {
                response.Append(contentItem.Text);
                response.Append(' ');
            }
        }
        return response.ToString();
    }

    public string GetCuratedResponse()
    {
        throw new NotImplementedException();
    }
}
