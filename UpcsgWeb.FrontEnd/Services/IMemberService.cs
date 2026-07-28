using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public interface IMemberService
{
    Task<List<MemberDto>> GetMembersAsync();
}
