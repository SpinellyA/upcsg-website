using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Abstractions;

namespace UpcsgWeb.Api.Features.Members;

public class DeleteMemberEndpoint(IMemberRepository members, IUnitOfWork uow) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/members/{id:guid}");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var member = await members.GetByIdAsync(Route<Guid>("id"), ct);
        if (member is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        members.Remove(member);
        await uow.SaveChangesAsync(ct);
        await Send.NoContentAsync(ct);
    }
}
