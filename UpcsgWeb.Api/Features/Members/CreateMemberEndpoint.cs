using FastEndpoints;
using MediatR;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Features.Members;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Members;

public class CreateMemberEndpoint(ISender sender) : Endpoint<MemberDto, MemberDto>
{
    public override void Configure()
    {
        Post("/members");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(MemberDto req, CancellationToken ct) =>
        await Send.OkAsync(await sender.Send(new CreateMemberCommand(req), ct), ct);
}
