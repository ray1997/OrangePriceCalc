using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using OrangeWebAPI;
using OrangeWebAPI.Helper;

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
                PermitLimit = 6,
                Window = TimeSpan.FromSeconds(10)
            });
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBrowserApp", policy =>
    {
        policy.WithOrigins("https://dcalc.toonwk.uk").AllowAnyMethod().AllowAnyHeader();
        policy.WithOrigins("https://dcalcdev.toonwk.uk").AllowAnyMethod().AllowAnyHeader();
    });
});
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.WriteIndented = true;
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonContext.Default);
});
//builder.Services.AddControllers();
var app = builder.Build();
app.UseRateLimiter();
app.UseCors("AllowBrowserApp");

app.MapGet("/api/init", () => OrangeAPICore.Initialize());
app.MapGet("/api/dbinfo", () => OrangeAPICore.GetDatabaseInfo());
app.MapGet("/api/{priceOrSku:decimal}", (decimal priceOrSku) => OrangeAPICore.GetPriceInfo(priceOrSku));
app.MapGet("/api/query/{priceOrSKU}/{begin}",
    (string priceOrSKU, string begin) => OrangeAPICore.QueryDiscountInfo(priceOrSKU, begin));
app.MapPost("api/query", (OrangeAPICore.QueryInfo query) => OrangeAPICore.QueryDiscountInfo(query));
app.Run();