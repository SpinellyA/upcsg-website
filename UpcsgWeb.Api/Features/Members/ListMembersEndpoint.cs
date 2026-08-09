using FastEndpoints;
using MediatR;
using UpcsgWeb.Application.Features.Members;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Members;

public class ListMembersEndpoint(ISender sender) : EndpointWithoutRequest<List<MemberDto>>
{
    public override void Configure()
    {
        Get("/members");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new ListMembersQuery(), ct), ct);
}
