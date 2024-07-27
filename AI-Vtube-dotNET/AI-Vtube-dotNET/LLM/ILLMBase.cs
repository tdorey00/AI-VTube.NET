namespace AI_Vtube_dotNET.LLM;

/// <summary>
/// A Generic Interface for LLM's to use to define common functionality
/// </summary>
public interface ILLMBase
{
    void InitLLM();

    void RecievePrompt(string prompt);

    string GetRawResponse();

    //TODO: Figure out how to model curated response parameters, maybe use a builder or something?
    string GetCuratedResponse();
}
