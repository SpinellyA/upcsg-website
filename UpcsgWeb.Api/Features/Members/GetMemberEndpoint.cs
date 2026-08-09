using FastEndpoints;
using MediatR;
using UpcsgWeb.Application.Features.Members;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Members;

public class GetMemberEndpoint(ISender sender) : EndpointWithoutRequest<MemberDto>
{
    public override void Configure()
    {
        Get("/members/{id:guid}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var found = await sender.Send(new GetMemberQuery(Route<Guid>("id")), ct);

        if (found is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(found, ct);
    }
}
