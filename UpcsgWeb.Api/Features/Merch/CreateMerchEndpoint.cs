using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Merch;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Merch;

public class CreateMerchEndpoint(ISender sender) : Endpoint<MerchItemDto, MerchItemDto>
{
    public override void Configure()
    {
        Post("/merch");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(MerchItemDto req, CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new CreateMerchCommand(req), ct), ct);
}
