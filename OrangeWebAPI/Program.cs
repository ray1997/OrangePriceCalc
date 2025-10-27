using OrangeWebAPI;

var builder = WebApplication.CreateBuilder(args);
//builder.Services.AddControllers();
var app = builder.Build();

app.MapGet("/api/init", () => OrangeAPICore.Initialize());
app.MapGet("/api/dbinfo", () => OrangeAPICore.GetDatabaseInfo());
app.MapGet("/api/{priceOrSku:decimal}", (decimal priceOrSku) => OrangeAPICore.GetPriceInfo(priceOrSku));

app.Run();