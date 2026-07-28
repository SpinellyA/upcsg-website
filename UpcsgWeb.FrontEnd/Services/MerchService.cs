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
        return
        [
            new MerchItemDto
            {
                Id = 1,
                Name = "UPCSG 2026-2027 Cosmic Hoodie",
                Description = "Midnight indigo pullover hoodie with galactic orchid embroidered logo.",
                Price = 750m,
                Variants = ["S", "M", "L", "XL"],
            },
            new MerchItemDto
            {
                Id = 2,
                Name = "Starlight Core Tote Bag",
                Description = "Canvas tote with the UPCSG starburst print in liquid chrome gold.",
                Price = 250m,
                Variants = ["One size"],
            },
            new MerchItemDto
            {
                Id = 3,
                Name = "Guild Enamel Pin Set",
                Description = "Set of 3 pins: UPCSG crest, mascot, and constellation icon.",
                Price = 180m,
                Variants = ["Set of 3"],
                InStock = false,
            },
            new MerchItemDto
            {
                Id = 4,
                Name = "Guild Statement Shirt",
                Description = "Honeydew white cotton tee with the starry-night crest printed across the chest.",
                Price = 450m,
                Variants = ["S", "M", "L", "XL", "2XL"],
            },
            new MerchItemDto
            {
                Id = 5,
                Name = "Nebula Lanyard",
                Description = "Woven lanyard in deep amethyst with a periwinkle guild repeat print.",
                Price = 120m,
                Variants = ["One size"],
            },
            new MerchItemDto
            {
                Id = 6,
                Name = "Sticker Pack: Cosmo Tech",
                Description = "Eight die-cut vinyl stickers from the Cosmo Tech graphic set. Laptop-safe.",
                Price = 90m,
                Variants = ["Pack of 8"],
            },
        ];
    }
}
