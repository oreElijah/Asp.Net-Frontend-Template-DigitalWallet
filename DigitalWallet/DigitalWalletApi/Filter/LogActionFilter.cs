using Microsoft.AspNetCore.Mvc.Filters;

namespace DigitalWalletApi.Filter
{
    public class LogActionFilter : IActionFilter
    {
        private readonly ILogger<LogActionFilter> _logger;

        public LogActionFilter(ILogger<LogActionFilter> logger)
        {
            _logger = logger;
        }

        public void OnActionExecuting(ActionExecutingContext _context)
        {
            _logger.LogInformation($"Executing {_context.ActionDescriptor.DisplayName}");
        }

        public void OnActionExecuted(ActionExecutedContext _context)
        {
            _logger.LogInformation($"Executed {_context.ActionDescriptor.DisplayName}");
        }
    }
}
