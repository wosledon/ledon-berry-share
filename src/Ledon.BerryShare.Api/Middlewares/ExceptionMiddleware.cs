using System;
using System.Text.Json;
using Ledon.BerryShare.Api.Controllers;

namespace Ledon.BerryShare.Api.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 200;

        var result = JsonSerializer.Serialize(new BerryResult
        {
            Code = BerryResult.StatusCodeEnum.Error,
            Message = ex.Message,
            Data = null
        });
        return context.Response.WriteAsync(result);
    }
}
