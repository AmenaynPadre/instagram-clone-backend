using System.Net;
using System.Text.Json;
using InstagramClone.Common;
using InstagramClone.Exceptions;

namespace InstagramClone.Middlewares;

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
            await HandleException(context, ex);
        }
    }

    private static async Task HandleException(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        var response = new Result<string>();

        switch (ex)
        {
            case BadRequestException:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = Result<string>.Fail(ex.Message);
                break;

            case UnauthorizedException:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response = Result<string>.Fail(ex.Message);
                break;
            
            case NotFoundException:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response = Result<string>.Fail(ex.Message);
                break;

            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response = Result<string>.Fail("Internal server error");
                break;
        }

        var json = JsonSerializer.Serialize(response);

        await context.Response.WriteAsync(json);
    }
}