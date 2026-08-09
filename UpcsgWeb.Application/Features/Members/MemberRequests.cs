using UpcsgWeb.Application.Abstractions;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.Application.Features.Members;

public record GetMemberQuery(Guid Id) : IQuery<MemberDto?>;

public record ListMembersQuery : IQuery<List<MemberDto>>;

public record CreateMemberCommand(MemberDto Member) : ICommand<MemberDto>;

public record UpdateMemberCommand(Guid Id, MemberDto Member) : ICommand<MemberDto>;

public record DeleteMemberCommand(Guid Id) : ICommand;
