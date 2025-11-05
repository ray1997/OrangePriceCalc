using System.Text.Json.Serialization;

namespace OrangeWebAPI.Helper;

[JsonSerializable(typeof(OrangeAPICore.DatabaseInfo))]
[JsonSerializable(typeof(OrangeAPICore.QueryInfo))]
[JsonSerializable(typeof(List<OrangeAPICore.DiscountStep>))]
[JsonSerializable((typeof(string)))]
[JsonSerializable((typeof(decimal)))]
[JsonSourceGenerationOptions(WriteIndented = true)]

public partial class AppJsonContext : JsonSerializerContext
{

}