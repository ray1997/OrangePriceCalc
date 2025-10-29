using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace Orange.Helper;

public static class HttpRequestor
{
    private static HttpClient _requestor = new()
    {
        BaseAddress = new Uri("https://oapi.toonwk.uk/api/")
    };

    public static async Task InitializeAPIServer()
    {
        var respond = await _requestor.GetAsync("init");
        respond.EnsureSuccessStatusCode();
    }

    public static async Task<decimal> GetPriceFromSKU(int sku)
    {
        var verify = sku.ToString();
        if (verify.Length != 8)
            return -1;
        if (!verify.StartsWith("60"))
            return -1;
        var response = await _requestor.GetAsync($"{sku}");
        response.EnsureSuccessStatusCode();
        var output = await response.Content.ReadAsStringAsync();
        var validPrice = decimal.TryParse(output, out var price);
        if (!validPrice)
            return -1;
        return price;
    }
}