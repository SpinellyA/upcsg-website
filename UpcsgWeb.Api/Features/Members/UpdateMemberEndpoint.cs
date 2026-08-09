using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Members;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Members;

public class UpdateMemberEndpoint(ISender sender) : Endpoint<MemberDto, MemberDto>
{
    public override void Configure()
    {
        Put("/members/{id:guid}");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(MemberDto req, CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new UpdateMemberCommand(Route<Guid>("id"), req), ct), ct);
}
