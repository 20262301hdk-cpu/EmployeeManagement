using Microsoft.AspNetCore.Mvc.Filters;

namespace EmployeeManagement.Filter;

public class LoggingActionFilter : IActionFilter
{
    private readonly ILogger<LoggingActionFilter> _logger;

    public LoggingActionFilter(ILogger<LoggingActionFilter> logger)
    {
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        // 事前実装 (削減版): T-10 アクション開始ログ出力
        var controller = context.RouteData.Values["controller"];
        var action = context.RouteData.Values["action"];
        var user = context.HttpContext.User?.Identity?.Name ?? "(anonymous)";
        _logger.LogInformation("Action start: {Controller}.{Action} User={User}", controller, action, user);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // 事前実装 (削減版): T-10 アクション終了ログ出力
        var controller = context.RouteData.Values["controller"];
        var action = context.RouteData.Values["action"];
        _logger.LogInformation("Action end: {Controller}.{Action}", controller, action);
    }
}
