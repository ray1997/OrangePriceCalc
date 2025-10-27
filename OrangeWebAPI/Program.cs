using System.Threading.RateLimiting;
using OrangeWebAPI;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory => new FixedWindowRateLimiterOptions()
            {
                AutoReplenishment = true,
                PermitLimit = 2,
                Window = TimeSpan.FromMilliseconds(500)
            });
    });
});
//builder.Services.AddControllers();
var app = builder.Build();

app.MapGet("/api/init", () => OrangeAPICore.Initialize());
app.MapGet("/api/dbinfo", () => OrangeAPICore.GetDatabaseInfo());
app.MapGet("/api/{priceOrSku:decimal}", (decimal priceOrSku) => OrangeAPICore.GetPriceInfo(priceOrSku));

app.Run();