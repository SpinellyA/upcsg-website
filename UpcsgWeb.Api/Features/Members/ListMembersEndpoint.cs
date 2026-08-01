using FastEndpoints;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Members;

public class ListMembersEndpoint(IMemberRepository members) : EndpointWithoutRequest<List<MemberDto>>
{
    public override void Configure()
    {
        Get("/members");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // The repository already applies roster order.
        var roster = await members.GetAllAsync(ct);
        await Send.OkAsync([.. roster.Select(m => m.ToDto())], ct);
    }
}
