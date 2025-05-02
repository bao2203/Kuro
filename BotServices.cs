using Kurohana.Helpers;
using Microsoft.Extensions.Logging;

namespace Kurohana
{
    public class BotServices
    {
        private readonly ILogger<BotServices> _logger;

        public BotServices(ILogger<BotServices> logger)
        {
            _logger = logger;
            LogHelper.Logger = logger; // set the logger to the LogHelper
        }

        public async Task StartAsync(CancellationToken token)
        {
            await LogHelper.LogInfoAsync("Bot is starting...", true); // log the bot is starting
        }
    }
}
