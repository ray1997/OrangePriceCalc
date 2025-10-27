using System.Text.Json.Serialization;

namespace OrangeWebAPI.Helper;

[JsonSerializable(typeof(OrangeAPICore.DatabaseInfo))]
[JsonSourceGenerationOptions(WriteIndented = true)]

public partial class AppJsonContext : JsonSerializerContext
{

}