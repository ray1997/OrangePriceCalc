using System.Text.Json.Serialization;
namespace OrangeWebAPI.Helper;

[JsonSerializable(typeof(List<OrangeController.BasicPriceInfo>))]
[JsonSourceGenerationOptions(WriteIndented = true)]
public partial class AppJsonContext : JsonSerializerContext
{
    
}