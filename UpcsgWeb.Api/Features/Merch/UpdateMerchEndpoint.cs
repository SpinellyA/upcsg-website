using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Merch;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Merch;

public class UpdateMerchEndpoint(ISender sender) : Endpoint<MerchItemDto, MerchItemDto>
{
    public override void Configure()
    {
        Put("/merch/{id:guid}");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(MerchItemDto req, CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new UpdateMerchCommand(Route<Guid>("id"), req), ct), ct);
}
