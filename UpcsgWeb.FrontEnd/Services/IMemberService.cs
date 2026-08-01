using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public interface IMemberService
{
    Task<List<MemberDto>> GetMembersAsync();

    /// <summary>
    /// One person by id, for the CMS page. Fetched directly rather than filtered out of
    /// the roster, so opening one officer doesn't pull the whole list down with them.
    /// </summary>
    Task<MemberDto?> GetMemberAsync(Guid id);
}
