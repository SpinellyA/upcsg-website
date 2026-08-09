using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Merch;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Merch;

public class AdminListMerchEndpoint(ISender sender) : EndpointWithoutRequest<List<MerchItemDto>>
{
    public override void Configure()
    {
        Get("/admin/merch");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new ListMerchQuery(), ct), ct);
}
