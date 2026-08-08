using System.Text.Json;
using System.Text.Json.Serialization;

namespace UpcsgWeb.FrontEnd.Services;

public static class UpcsgJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
