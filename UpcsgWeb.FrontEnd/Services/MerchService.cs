using System.Net.Http.Json;
using UpcsgWeb.Shared.Contracts;

namespace UpcsgWeb.FrontEnd.Services;

public class MerchService(HttpClient http, ApiOptions options) : IMerchService
{
    public async Task<List<MerchItemDto>> GetMerchAsync()
    {
        // Live when an API is configured; the seed below keeps the public site
        // renderable standalone.
        if (options.IsConfigured)
        {
            return await http.GetFromJsonAsync<List<MerchItemDto>>("api/merch", UpcsgJson.Options) ?? [];
        }

        return SeedData();
    }

    public async Task<MerchItemDto?> GetMerchItemAsync(int id)
    {
        if (!options.IsConfigured)
        {
            return SeedData().FirstOrDefault(m => m.Id == id);
        }

        // A bad id in the URL is an ordinary answer, not an exception to catch.
        var response = await http.GetAsync($"api/merch/{id}");

        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<MerchItemDto>(UpcsgJson.Options);
    }

    /// <summary>
    /// Offline sample. Variants price independently here too, so the "from" pricing and the
    /// variant switcher can be exercised without a database behind them.
    /// </summary>
    private static List<MerchItemDto> SeedData()
    {
        var items = new List<MerchItemDto>
        {
            new()
            {
                Id = 1,
                Name = "UPCSG 2026-2027 Cosmic Hoodie",
                Description = "Midnight indigo pullover hoodie with galactic orchid embroidered logo.",
                Price = 750m,
                Variants =
                [
                    new MerchVariantDto { Id = 1, Name = "S", Price = 750m },
                    new MerchVariantDto { Id = 2, Name = "M", Price = 750m },
                    new MerchVariantDto { Id = 3, Name = "L", Price = 780m },
                    new MerchVariantDto { Id = 4, Name = "XL", Price = 820m, Description = "Runs generous through the shoulders." },
                ],
            },
            new()
            {
                Id = 2,
                Name = "Starlight Core Tote Bag",
                Description = "Canvas tote with the UPCSG starburst print in liquid chrome gold.",
                Price = 250m,
                Variants = [new MerchVariantDto { Id = 5, Name = "One size", Price = 250m }],
            },
            new()
            {
                Id = 3,
                Name = "Guild Enamel Pin Set",
                Description = "Set of 3 pins: UPCSG crest, mascot, and constellation icon.",
                Price = 180m,
                InStock = false,
                Variants = [new MerchVariantDto { Id = 6, Name = "Set of 3", Price = 180m }],
            },
            new()
            {
                Id = 4,
                Name = "Guild Statement Shirt",
                Description = "Honeydew white cotton tee with the starry-night crest printed across the chest.",
                Price = 450m,
                Variants =
                [
                    new MerchVariantDto { Id = 7, Name = "S", Price = 450m },
                    new MerchVariantDto { Id = 8, Name = "M", Price = 450m },
                    new MerchVariantDto { Id = 9, Name = "L", Price = 450m },
                    new MerchVariantDto { Id = 10, Name = "XL", Price = 480m },
                    new MerchVariantDto { Id = 11, Name = "2XL", Price = 510m },
                ],
            },
            new()
            {
                Id = 5,
                Name = "Nebula Lanyard",
                Description = "Woven lanyard in deep amethyst with a periwinkle guild repeat print.",
                Price = 120m,
                Variants = [new MerchVariantDto { Id = 12, Name = "One size", Price = 120m }],
            },
            new()
            {
                Id = 6,
                Name = "Sticker Pack: Cosmo Tech",
                Description = "Eight die-cut vinyl stickers from the Cosmo Tech graphic set. Laptop-safe.",
                Price = 90m,
                Variants = [new MerchVariantDto { Id = 13, Name = "Pack of 8", Price = 90m }],
            },
        };

        // The API computes these; offline we have to fill them in or listings would show
        // a base price the variants don't agree with.
        foreach (var item in items)
        {
            item.PriceFrom = item.Variants.Count == 0 ? item.Price : item.Variants.Min(v => v.Price);
            item.HasPriceRange = item.Variants.Select(v => v.Price).Distinct().Count() > 1;
        }

        return items;
    }
}
