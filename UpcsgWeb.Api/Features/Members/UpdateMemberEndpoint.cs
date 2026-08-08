using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Api.Features.Members;

public class UpdateMemberEndpoint(IMemberRepository members, IUnitOfWork uow)
    : Endpoint<MemberDto, MemberDto>
{
    public override void Configure()
    {
        Put("/members/{id:guid}");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(MemberDto req, CancellationToken ct)
    {
        var member = await members.GetByIdAsync(Route<Guid>("id"), ct);
        if (member is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        try
        {
            member.Update(req.Name, req.Role, req.Committee, req.DisplayOrder);
            member.SetProfile(req.PhotoUrl, req.Quote, req.Bio);
        }
        catch (DomainException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
            return;
        }

        await uow.SaveChangesAsync(ct);
        await Send.OkAsync(member.ToDto(), ct);
    }
}
