using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OrangeWebAPI.Helper;

namespace OrangeWebAPI;

public static class OrangeAPICore
{
    public static Dictionary<int, decimal>? LoadedPriceInfo { get; set; }

    private static string _databasePath = string.Empty;
    public static string DatabasePath
    {
        get
        {
            if (string.IsNullOrEmpty(_databasePath))
                _databasePath = Config.Get(nameof(DatabasePath), "/storage/media/configs/n8n/database/");

            return _databasePath;
        }
    }

    private static long LatestDatabaseUpdate = -1;

    public static IResult Initialize()
    {
        try
        {
            //No longer read and rewrite to json > load into memory instead:
            // Step 1: List all CSV files
            var csvFiles = Directory.GetFiles(DatabasePath, "*.CSV");
            if (csvFiles.Length == 0)
                return Results.NoContent();

            // Step 2: Find the file with the highest numeric suffix (yyyyMMdd)
            var latestInfo = csvFiles
                .Select(f => new
                {
                    Path = f,
                    DateNum = GetTrailingNumber(Path.GetFileNameWithoutExtension(f))
                })
                .Where(x => x.DateNum != null)
                .OrderByDescending(x => x.DateNum)
                .FirstOrDefault();
            if (latestInfo is null)
                return Results.Json("Failed finding latest database files", AppJsonContext.Default.String);
            var latestFile = latestInfo.Path;
            if (latestInfo.DateNum.HasValue && latestInfo.DateNum.Value == LatestDatabaseUpdate)
                return Results.Json("Database updated!", AppJsonContext.Default.String);
            LatestDatabaseUpdate = latestInfo.DateNum ?? -1;

            if (string.IsNullOrEmpty(latestFile)) //No CSV already thrown NotFound, this should never happen
                latestFile = string.Empty;

            // Step 3: Read CSV
            var lines = System.IO.File.ReadAllLines(latestFile);

            //Initialize list
            LoadedPriceInfo ??= [];

            // Assuming CSV columns are comma-separated
            foreach (var line in lines.Skip(1)) // skip header
            {
                var cols = line.Split(',');
                if (cols.Length < 10) continue;

                if (int.TryParse(cols[0], out var sku) &&
                    decimal.TryParse(cols[10], NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                {
                    if (!LoadedPriceInfo.TryAdd(sku, price))
                        LoadedPriceInfo[sku] = price;
                }
            }

            // Step 7: Return OK
            return Results.Json("Database updated!", AppJsonContext.Default.String);
        }
        catch
        {
            return Results.StatusCode(500);
        }
    }

    public static IResult GetPriceInfo(decimal PriceOrSKU)
    {
        if (LoadedPriceInfo == null)
            return Results.NoContent();
        
        if (PriceOrSKU is >= 60000000.00m and <= 61000000.00m && decimal.IsInteger(PriceOrSKU))
        {
            return !LoadedPriceInfo.ContainsKey((int)PriceOrSKU) ? 
                Results.Json("Database don't have this item price info", AppJsonContext.Default.String) : 
                Results.Json(LoadedPriceInfo[(int)PriceOrSKU], AppJsonContext.Default.Decimal);
        }

        return Results.NoContent();
    }

    public record DatabaseInfo(int UpdateDate, int Items);

    public static IResult GetDatabaseInfo()
    {
        return Results.Json(new DatabaseInfo((int)LatestDatabaseUpdate, LoadedPriceInfo?.Count ?? 0),
            AppJsonContext.Default.DatabaseInfo);
    }

    private static long? GetTrailingNumber(string fileName)
    {
        // Extract last 8 digits (yyyyMMdd)
        var name = Path.GetFileNameWithoutExtension(fileName);
        var extractedName = name[^8..];
        //var digits = new string(fileName.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
        return long.TryParse(extractedName, out var num) ? num : null;
    }

    public static IResult QueryDiscountInfo(string initialPrice, string initialBegin)
    {
        if (LoadedPriceInfo is null)
            return Results.NoContent();
        
        var validPrice = decimal.TryParse(initialPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var price);
        if (!validPrice)
            return Results.NoContent();

        var validDate = DateTime.TryParse(initialBegin, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal,
            out var begin);
        if (!validDate)
            return Results.NoContent();

        //Decide if this is price or SKU
        if (price >= 60000000 && decimal.IsInteger(price) && price <= 61000000)
        {
            //SKU
            var sku = Convert.ToInt32(price);
            price = LoadedPriceInfo[sku];
        }
        
        var steps = new List<DiscountStep>();
        for (var i = 1; i < 8; i++)
        {
            var dcrInfo = GetDiscountRange(begin, i - 1); //Discount range info
            steps.Add(new DiscountStep(price, GetDiscountSteps(i), dcrInfo.range, dcrInfo.withinRange));
        }
        return Results.Json(steps, AppJsonContext.Default.ListDatabaseInfo);
    }

    private record DiscountStep(decimal FullPrice, decimal Percent, string Range, bool InRange)
    {
        public decimal DiscountedPrice => Math.Ceiling(FullPrice - (FullPrice * Percent));

        public string PercentageDisplay
        {
            get
            {
                var result = $"{Percent * 100}%";
                return result == "100%" ? "-" : result;
            }
        }
    }

    private static (string range, bool withinRange) GetDiscountRange(DateTime originalDate, int from, int to = -1)
    {
        var begin = from == 0 ? originalDate : originalDate.AddMonths(from).AddDays(1);
        if (to == -1)
            to = from + 1;
        var end = originalDate.AddMonths(to);
        if (from == 0)
            end = end.AddDays(1);

        var today = DateTime.Today;
        var inRange = today > begin && today < end;
        
        if (from == 0 && to == 1) //First month
        {
            return ($"{begin:dd/MM/yyyy} - {end:dd/MM/yyyy}", inRange);
        }
        
        return to == int.MaxValue ? //Expiry date
            ($"{begin:dd/MM/yyyy}", inRange) : ($"{begin:dd/MM/yyyy} - {end:dd/MM/yyyy}", inRange);
    }
    
    private static decimal GetDiscountSteps(int step) =>
        step switch
        {
            1 => 0.3m,
            2 => 0.5m,
            3 or 4 or 5 => 0.7m,
            6 => 0.95m,
            _ => 1m
        };
}