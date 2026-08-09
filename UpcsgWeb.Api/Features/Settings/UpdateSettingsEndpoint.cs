using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Settings;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Settings;

public class UpdateSettingsEndpoint(ISender sender)
    : Endpoint<UpdateSiteSettingsRequest, SiteSettingsDto>
{
    public override void Configure()
    {
        Put("/settings");
        Policies(AuthPolicies.ExeCom);
        Summary(s => s.Summary = "Pin the events calendar to a month, or follow the real one.");
    }

    public override async Task HandleAsync(UpdateSiteSettingsRequest req, CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new UpdateSettingsCommand(req), ct), ct);
}
