using System;
using System.Text.Json;
using Ledon.BerryShare.Api.Controllers;
using Ledon.BerryShare.Shared;

namespace Ledon.BerryShare.Api.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);

            // 处理 401 未授权
            if (context.Response.StatusCode == 401)
            {
                context.Response.StatusCode = 200;
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new BerryResult
                {
                    Code = BerryResult.StatusCodeEnum.Unauthorized,
                    Message = "未授权访问"
                });
                await context.Response.WriteAsync(result);
            }
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        if (context.Response.HasStarted)
        {
            // 只记录日志，不回写内容
            // 例如：Log.Error(ex, "响应已开始，无法回写异常信息");
            _logger.LogError(ex, "响应已开始，无法回写异常信息");

            return Task.CompletedTask;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 200;

        var result = JsonSerializer.Serialize(new BerryResult
        {
            Code = BerryResult.StatusCodeEnum.Error,
            Message = ex.Message,
        });
        return context.Response.WriteAsync(result);
    }
}
