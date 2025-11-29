using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using DiscountCalcAPI;
using DiscountCalcAPI.Helper;
using Swashbuckle.AspNetCore;

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

//OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//builder.Services.AddControllers();
var app = builder.Build();

if (app.Environment.IsDevelopment() || true) // enable even in production for testing
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Discount API v1");
        c.RoutePrefix = "docs"; // Swagger UI will be at /docs
    });
}


app.UseRateLimiter();
app.UseCors("AllowBrowserApp");

app.MapGet("/api/init", () => OrangeAPICore.Initialize())
    .WithName("Initialize")
    .WithSummary("Initialize the API; need to run this first, if 'dbinfo' return -1");
app.MapGet("/api/dbinfo", () => OrangeAPICore.GetDatabaseInfo())
    .WithName("DatabaseInfo")
    .WithSummary("Query database info; Return a json with a date of data and amount of price data.\r\nIf data wasn't initialize, it will return -1 on data of date");
app.MapGet("/api/{priceOrSku:decimal}", (decimal priceOrSku) => OrangeAPICore.GetPriceInfo(priceOrSku))
    .WithName("GetPriceOfSKU")
    .WithSummary("Return a decimal price of item sku");
app.MapGet("/api/query/{priceOrSKU}/{begin}",
    (string priceOrSKU, string begin) => OrangeAPICore.QueryDiscountInfo(priceOrSKU, begin))
    .WithName("QueryDiscountInfo")
    .WithSummary("Return a json of discount info, request sku or price,\r\n and a begin date in 8 digit format yyyyMMdd");
app.Run();