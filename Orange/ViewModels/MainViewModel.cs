using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using DebounceThrottle;
using Orange.Helper;

namespace Orange.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public MainViewModel()
    {
        UpdateSteps(0, DateTime.Today.AddDays(-1));
    }

    public void UpdateSteps(decimal fullPrice, DateTime beginDiscount)
    {
        Steps = [];
        for (var i = 1; i < 8; i++)
        {
            var dcrInfo = GetDiscountRange(beginDiscount, i - 1); //Discount range info
            Steps.Add(new DiscountStep(fullPrice, GetDiscountSteps(i), dcrInfo.range, dcrInfo.withinRange));
        }
        HighlightToday();
    }

    private decimal GetDiscountSteps(int step) =>
        step switch
        {
            1 => 0.3m,
            2 => 0.5m,
            3 or 4 or 5 => 0.7m,
            6 => 0.95m,
            _ => 1m
        };

    public static (string range, bool withinRange) GetDiscountRange(DateTime originalDate, int from, int to = -1)
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
        
        if (to == int.MaxValue) //Expiry date
        {
            return ($"{begin:dd/MM/yyyy}", inRange);
        }

        return ($"{begin:dd/MM/yyyy} - {end:dd/MM/yyyy}", inRange);
    }
    
    [ObservableProperty]
    string priceInput = "";
    
    partial void OnPriceInputChanged(string value)
    {
        _ = RefreshStepsAsync(value, DateInput?.DateTime ?? DateTime.Today);
    }

    private async Task RefreshStepsAsync(string value, DateTime time)
    {
        if (value.Length == 8 && value.StartsWith("60"))
        {
            //SKU
            var validSku = int.TryParse(value, out var sku);
            if (!validSku)
                return;

            var actualPrice = await HttpRequestor.GetPriceFromSKU(sku);
            UpdateSteps(actualPrice, time);
            return;
        }

        var validPrice = decimal.TryParse(value, out var price);
        if (!validPrice)
        {
            return;
        }
        UpdateSteps(price, time);
    }

    private void HighlightToday()
    {
        if (DateInput is null)
            return;

        var begin = DateInput.Value.DateTime;
        var today = DateTime.Today;
        if (today >= begin)
        {
            Highlighter = 0;
        }

        Highlighter = (today.Year + today.Month) - (begin.Year + begin.Month);
        if (begin.Day > today.Day)
            Highlighter--;
    }

    [ObservableProperty] DateTimeOffset? dateInput = DateTimeOffset.Now.Subtract(TimeSpan.FromDays(1));

    partial void OnDateInputChanged(DateTimeOffset? value)
    {
        if (value is null)
            return;
        
        _ = RefreshStepsAsync(PriceInput, value.Value.DateTime);
    }

    [ObservableProperty] private ObservableCollection<DiscountStep> steps = [];

    [ObservableProperty] private int highlighter = -1;
}

public record DiscountStep(decimal FullPrice, decimal Percent, string Range, bool InRange)
{
    public decimal DiscountedPrice => Math.Ceiling(FullPrice - (FullPrice * Percent));

    public string PercentageDisplay
    {
        get
        {
            string result = $"{Percent * 100}%";
            if (result == "100%")
                return "-";
            return result;
        }
    }
}