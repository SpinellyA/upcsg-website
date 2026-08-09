using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Api;

public static class ExceptionMapping
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    public static IApplicationBuilder UseDomainExceptionMapping(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (Exception ex) when (ex is DomainException or NotFoundException
                or ForbiddenException or UnauthorizedException or ValidationException)
            {
                if (context.Response.HasStarted)
                {
                    throw;
                }

                var status = ex switch
                {
                    NotFoundException => StatusCodes.Status404NotFound,
                    ForbiddenException => StatusCodes.Status403Forbidden,
                    UnauthorizedException => StatusCodes.Status401Unauthorized,
                    ValidationException => StatusCodes.Status400BadRequest,

                    _ => StatusCodes.Status409Conflict,
                };

                var errors = ex is ValidationException validation
                    ? validation.Errors
                        .GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())
                    : new Dictionary<string, string[]> { ["generalErrors"] = [ex.Message] };

                context.Response.Clear();
                context.Response.StatusCode = status;
                context.Response.ContentType = "application/problem+json";

                await context.Response.WriteAsJsonAsync(
                    new
                    {
                        statusCode = status,
                        message = "One or more errors occurred!",
                        errors,
                    },
                    Json);
            }
        });
}
