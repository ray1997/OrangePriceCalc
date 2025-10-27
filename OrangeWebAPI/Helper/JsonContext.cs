using System.Text.Json.Serialization;

namespace OrangeWebAPI.Helper;

[JsonSerializable(typeof(OrangeAPICore.DatabaseInfo))]
[JsonSerializable(typeof(OrangeAPICore.InitializeStatus))]
[JsonSourceGenerationOptions(WriteIndented = true)]

public partial class AppJsonContext : JsonSerializerContext
{

}