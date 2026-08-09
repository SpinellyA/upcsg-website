using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Application.Mapping;
using UpcsgWeb.Shared.Contracts;
using DomainMember = UpcsgWeb.Domain.Content.Member;
using DomainMemberCategory = UpcsgWeb.Domain.Content.MemberCategory;

namespace UpcsgWeb.Application.Features.Members;

public class GetMemberQueryHandler(IUnitOfWork uow) : IQueryHandler<GetMemberQuery, MemberDto?>
{
    public async Task<MemberDto?> Handle(GetMemberQuery query, CancellationToken ct)
    {
        var found = await uow.Members.GetByIdAsync(query.Id, ct);
        return found?.ToDto();
    }
}

public class ListMembersQueryHandler(IUnitOfWork uow)
    : IQueryHandler<ListMembersQuery, List<MemberDto>>
{
    public async Task<List<MemberDto>> Handle(ListMembersQuery query, CancellationToken ct)
    {
        var roster = await uow.Members.GetAllAsync(ct);
        return [.. roster.Select(m => m.ToDto())];
    }
}

public class CreateMemberCommandHandler(IUnitOfWork uow)
    : ICommandHandler<CreateMemberCommand, MemberDto>
{
    public async Task<MemberDto> Handle(CreateMemberCommand command, CancellationToken ct)
    {
        var dto = command.Member;

        var member = DomainMember.Create(
            dto.Name,
            dto.Role,
            dto.Category == MemberCategory.Faculty
                ? DomainMemberCategory.Faculty
                : DomainMemberCategory.ExeCom,
            dto.Committee,
            dto.DisplayOrder);

        member.SetProfile(dto.PhotoUrl, dto.Quote, dto.Bio);

        uow.Members.Add(member);
        await uow.SaveChangesAsync(ct);

        return member.ToDto();
    }
}

public class UpdateMemberCommandHandler(IUnitOfWork uow)
    : ICommandHandler<UpdateMemberCommand, MemberDto>
{
    public async Task<MemberDto> Handle(UpdateMemberCommand command, CancellationToken ct)
    {
        var member = await uow.Members.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("That member");

        var dto = command.Member;
        member.Update(dto.Name, dto.Role, dto.Committee, dto.DisplayOrder);
        member.SetProfile(dto.PhotoUrl, dto.Quote, dto.Bio);

        await uow.SaveChangesAsync(ct);

        return member.ToDto();
    }
}

public class DeleteMemberCommandHandler(IUnitOfWork uow) : ICommandHandler<DeleteMemberCommand>
{
    public async Task Handle(DeleteMemberCommand command, CancellationToken ct)
    {
        var member = await uow.Members.GetByIdAsync(command.Id, ct)
            ?? throw new NotFoundException("That member");

        uow.Members.Remove(member);
        await uow.SaveChangesAsync(ct);
    }
}
