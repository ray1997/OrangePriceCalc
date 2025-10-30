using System.Text.Json.Serialization;

namespace OrangeWebAPI.Helper;

[JsonSerializable(typeof(OrangeAPICore.DatabaseInfo))]
[JsonSerializable((typeof(string)))]
[JsonSerializable((typeof(decimal)))]
[JsonSourceGenerationOptions(WriteIndented = true)]

public partial class AppJsonContext : JsonSerializerContext
{

}