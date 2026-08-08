using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Orders.ReleaseConfirmed;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Orders;

public class ReleaseConfirmedEndpoint(ISender sender) : EndpointWithoutRequest<ReleaseConfirmedDto>
{
    public override void Configure()
    {
        Post("/orders/release-confirmed");
        Policies(AuthPolicies.ExeCom);
        Summary(s => s.Summary = "Mark every Acknowledged order as Released.");
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new ReleaseConfirmedCommand(), ct), ct);
}
