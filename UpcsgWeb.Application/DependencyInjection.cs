using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using UpcsgWeb.Application.Behaviors;

namespace UpcsgWeb.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceCollectionExtensions).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));

            // After validation, so it only ever runs against a response that exists. It
            // wraps the handler, so the presigned URL is minted from the order as saved.
            cfg.AddOpenBehavior(typeof(ResolveReceiptUrlBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        return services;
    }
}
