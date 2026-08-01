using FastEndpoints;
using UpcsgWeb.Api.Auth;
using UpcsgWeb.Api.Mapping;
using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Domain.Common;
using UpcsgWeb.Shared.Contracts;
using DomainMember = UpcsgWeb.Domain.Content.Member;
using DomainMemberCategory = UpcsgWeb.Domain.Content.MemberCategory;

namespace UpcsgWeb.Api.Features.Members;

public class CreateMemberEndpoint(IMemberRepository members, IUnitOfWork uow)
    : Endpoint<MemberDto, MemberDto>
{
    public override void Configure()
    {
        Post("/members");
        Policies(AuthPolicies.ExeCom);
    }

    public override async Task HandleAsync(MemberDto req, CancellationToken ct)
    {
        DomainMember member;
        try
        {
            member = DomainMember.Create(
                req.Name,
                req.Role,
                req.Category == MemberCategory.Faculty
                    ? DomainMemberCategory.Faculty
                    : DomainMemberCategory.ExeCom,
                req.Committee,
                req.DisplayOrder);

            member.SetProfile(req.PhotoUrl, req.Quote, req.Bio);
        }
        catch (DomainException ex)
        {
            AddError(ex.Message);
            await Send.ErrorsAsync(400, ct);
            return;
        }

        members.Add(member);
        await uow.SaveChangesAsync(ct);
        await Send.OkAsync(member.ToDto(), ct);
    }
}
