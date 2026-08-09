using FastEndpoints;
using MediatR;
using UpcsgWeb.Application.Features.Settings;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Settings;

public class GetSettingsEndpoint(ISender sender) : EndpointWithoutRequest<SiteSettingsDto>
{
    public override void Configure()
    {
        Get("/settings");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new GetSettingsQuery(), ct), ct);
}
