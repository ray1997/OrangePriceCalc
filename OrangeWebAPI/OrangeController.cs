using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using OrangeWebAPI.Helper;

namespace OrangeWebAPI;

[ApiController]
[Route("/api")]
public class OrangeController : ControllerBase
{
    public Dictionary<int, decimal>? LoadedPriceInfo { get; set; }

    private string _databasePath = string.Empty;
    public string DatabasePath
    {
        get
        {
            if (string.IsNullOrEmpty(_databasePath))
                _databasePath = Config.Get(nameof(DatabasePath), "/storage/media/configs/n8n/database/");

            return _databasePath;
        }
    }

    [HttpGet("/{PriceOrSKU:decimal}")]
    public IActionResult Get(decimal PriceOrSKU)
    {
        if (LoadedPriceInfo == null)
            return NoContent();
        if (PriceOrSKU is >= 60000000.00m and <= 61000000.00m && decimal.IsInteger(PriceOrSKU))
        {
            if (!LoadedPriceInfo.ContainsKey((int)PriceOrSKU))
                return NotFound("Database don't have this item price info");
            return Ok(LoadedPriceInfo[(int)PriceOrSKU]);
        }
        return Ok(0);
    }

    private record DatabaseInfo(int UpdateDate, int Items);

    [HttpGet("/dbinfo")]
    public IActionResult LatestUpdate()
    {
        return Ok(new DatabaseInfo((int)LatestDatabaseUpdate, LoadedPriceInfo?.Count ?? 0));
    }

    public long LatestDatabaseUpdate = -1;

    [HttpGet("/init")]
    public IActionResult Initialize()
    {
        try
        {
            //No longer read and rewrite to json > load into memory instead:
            
            // Step 1: List all CSV files
            var csvFiles = Directory.GetFiles(DatabasePath, "*.CSV");
            if (csvFiles.Length == 0)
                return NotFound("No CSV files found.");

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
                return NotFound("Failed finding latest database files");
            var latestFile = latestInfo.Path;
            if (latestInfo.DateNum.HasValue && latestInfo.DateNum.Value == LatestDatabaseUpdate)
                return Ok("Database updated!");
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
            return Ok(new
            {
                Message = "Initialization completed; Database updated!"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Initialization failed: {ex.Message}\r\n{ex.StackTrace}");
        }
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