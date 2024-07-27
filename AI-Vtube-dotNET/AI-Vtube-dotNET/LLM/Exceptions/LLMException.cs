namespace AI_Vtube_dotNET.LLM.Exceptions;

public sealed class LLMException : Exception
{
    public string? LLMName { get; }

    public LLMException() { }

    public LLMException(string message)
        : base(message) { }

    public LLMException(string message, Exception inner)
        : base(message, inner) { }

    public LLMException(string message, string llmName)
        : this(message)
    {
        LLMName = LLMName;
    }
}
