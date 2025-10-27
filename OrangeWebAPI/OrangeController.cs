using System.Globalization;
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

    public record InitializeStatus(string message);
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
            if (!LoadedPriceInfo.ContainsKey((int)PriceOrSKU))
                return Results.NotFound(new InitializeStatus("Database don't have this item price info"));
            return Results.Ok(LoadedPriceInfo[(int)PriceOrSKU]);
        }
        return Results.Ok(0);
    }

    public record DatabaseInfo(int UpdateDate, int Items);

    public static IResult GetDatabaseInfo()
    {
        return Results.Ok(new DatabaseInfo((int)LatestDatabaseUpdate, LoadedPriceInfo?.Count ?? 0));
    }

    private static long? GetTrailingNumber(string fileName)
    {
        // Extract last 8 digits (yyyyMMdd)
        var name = Path.GetFileNameWithoutExtension(fileName);
        var extractedName = name[^8..];
        //var digits = new string(fileName.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
        return long.TryParse(extractedName, out var num) ? num : null;
    }
}