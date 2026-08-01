using System.Text.Json;
using System.Text.Json.Serialization;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Common;

namespace UpcsgWeb.Api;

/// <summary>
/// The single place where a thrown rule becomes an HTTP status code.
///
/// Every endpoint used to repeat the same try/catch around a domain call, which meant a
/// rule that started throwing from a new place quietly became a 500 until someone
/// noticed. Handlers now just throw, and this decides what the wire sees.
/// </summary>
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
            catch (Exception ex) when (ex is DomainException or NotFoundException or ForbiddenException)
            {
                if (context.Response.HasStarted)
                {
                    // Too late to change the status line; re-throwing at least keeps the
                    // failure visible in logs instead of truncating the body silently.
                    throw;
                }

                var status = ex switch
                {
                    NotFoundException => StatusCodes.Status404NotFound,
                    ForbiddenException => StatusCodes.Status403Forbidden,

                    // 409, not 400: the request is well-formed, it just conflicts with
                    // the current state. A 400 would tell the client to fix its payload.
                    _ => StatusCodes.Status409Conflict,
                };

                context.Response.Clear();
                context.Response.StatusCode = status;
                context.Response.ContentType = "application/problem+json";

                // Shaped like FastEndpoints' own validation failures so the client's
                // error reader does not need a second branch.
                await context.Response.WriteAsJsonAsync(
                    new
                    {
                        statusCode = status,
                        message = "One or more errors occurred!",
                        errors = new Dictionary<string, string[]>
                        {
                            ["generalErrors"] = [ex.Message],
                        },
                    },
                    Json);
            }
        });
}
