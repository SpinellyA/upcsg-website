using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public interface IMerchService
{
    Task<List<MerchItemDto>> GetMerchAsync();
}
