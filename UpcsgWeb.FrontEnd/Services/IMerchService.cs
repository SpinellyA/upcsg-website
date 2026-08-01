using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public interface IMerchService
{
    Task<List<MerchItemDto>> GetMerchAsync();

    /// <summary>
    /// One item by id, for the detail page. Fetched directly rather than filtered out of
    /// the catalogue, so a shared product link survives the store growing.
    /// </summary>
    Task<MerchItemDto?> GetMerchItemAsync(Guid id);
}
