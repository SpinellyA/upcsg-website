using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Domain.Abstractions;

namespace UpcsgWeb.Api.Features.Members;

public class DeleteMemberEndpoint(IMemberRepository members, IUnitOfWork uow) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/members/{id:int}");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var member = await members.GetByIdAsync(Route<int>("id"), ct);
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
