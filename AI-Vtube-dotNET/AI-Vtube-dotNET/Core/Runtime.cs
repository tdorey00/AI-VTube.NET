using Microsoft.Extensions.Logging;

namespace AI_Vtube_dotNET.Core
{
    public class Runtime
    {
        private readonly ILogger _logger;
        public Runtime(ILogger<Runtime> logger) 
        { 
            _logger = logger;
        }

        //This becomes async one day
        public void RunAsync()
        {
            _logger.LogInformation("Hi");
        }
    }
}
