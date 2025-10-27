using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using OrangeWebAPI.Helper;

namespace OrangeWebAPI;

[ApiController]
[Route("/api")]
public class OrangeController : ControllerBase
{
    public record BasicPriceInfo(int Sku, decimal Price);

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


    private string _latestJsonPath = string.Empty;
    public string LatestJsonPath
    {
        get
        {
            if (string.IsNullOrEmpty(_latestJsonPath))
                _latestJsonPath = Config.Get(nameof(LatestJsonPath), "/storage/media/configs/n8n/database/latest.json");
            return _latestJsonPath;
        }
    }

    [Route("init")]
    [HttpGet]
    public IActionResult Initialize()
    {
        try
        {
            var latestReadName = string.Empty;
            DirectoryInfo di = new DirectoryInfo(DatabasePath);
            var readInfo = di.GetFiles("latest.read");
            if (readInfo.Length > 0)
            {
                latestReadName = System.IO.File.ReadAllText(readInfo[0].FullName);
            }
            else if (readInfo.Length == 0)
            {
                System.IO.File.WriteAllText(di.FullName + "/latest.json", string.Empty);
                latestReadName = string.Empty;
            }
            
            // Step 1: List all CSV files
            var csvFiles = Directory.GetFiles(DatabasePath, "*.CSV");
            if (csvFiles.Length == 0)
                return NotFound("No CSV files found.");

            // Step 2: Find the file with the highest numeric suffix (yyyyMMdd)
            var latestFile = csvFiles
                .Select(f => new
                {
                    Path = f,
                    DateNum = GetTrailingNumber(Path.GetFileNameWithoutExtension(f))
                })
                .Where(x => x.DateNum != null)
                .OrderByDescending(x => x.DateNum)
                .FirstOrDefault()?.Path;
            
            if (string.IsNullOrEmpty(latestFile))
                latestFile = string.Empty;
            
            if (latestFile == latestReadName)
                return Ok("Server updated!");
            
            // Step 3: Read CSV
            var lines = System.IO.File.ReadAllLines(latestFile);
            
            var items = new List<BasicPriceInfo>();

            // Assuming CSV columns are comma-separated
            foreach (var line in lines.Skip(1)) // skip header
            {
                var cols = line.Split(',');
                if (cols.Length < 10) continue;

                if (int.TryParse(cols[0], out var sku) &&
                    decimal.TryParse(cols[10], NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
                {
                    items.Add(new BasicPriceInfo(sku, price));
                }
            }

            // Step 4 + 6: Save to latest.json
            var json = JsonSerializer.Serialize(items, AppJsonContext.Default.ListBasicPriceInfo);
            System.IO.File.WriteAllText(_latestJsonPath, json);
            
            //Save latest read info
            System.IO.File.WriteAllText(readInfo.First().FullName, latestFile);

            // Step 7: Return OK
            return Ok(new
            {
                Message = "Initialization completed.",
                CsvFile = Path.GetFileName(latestFile),
                ItemCount = items.Count
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
        var digits = new string(fileName.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
        return long.TryParse(digits, out var num) ? num : null;
    }
}