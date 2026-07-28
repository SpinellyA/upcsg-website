using System.Text.Json;
using System.Text.Json.Serialization;

namespace UpcsgWeb.FrontEnd.Services;

/// <summary>
/// Serializer settings shared by every API call.
///
/// Must mirror the API's configuration: enums go over the wire as names. Relying on
/// the numeric default would work only while both sides agree, and would break the
/// moment a value was inserted into the middle of an enum.
/// </summary>
public static class UpcsgJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };
}
