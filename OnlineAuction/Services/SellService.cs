using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class SellService : ISellService
{
    public CreateAuctionViewModel BuildCreateForm()
    {
        var model = new CreateAuctionViewModel
        {
            StartDate = DateTime.Now.AddHours(1),
            EndDate = DateTime.Now.AddDays(7),
            BidStep = 50,
            Condition = "New",
            AuctionType = "Normal",
            Language = "English",
            AuctionEventName = "RareCard Vault: Premium Trading Card Auction 2026"
        };

        PopulateOptions(model);
        return model;
    }

    public CreateBuyNowViewModel BuildBuyNowForm()
    {
        var model = new CreateBuyNowViewModel
        {
            Condition = "New",
            Language = "English"
        };

        PopulateOptions(model);
        return model;
    }

    public void PopulateOptions(CreateAuctionViewModel model)
    {
        model.Categories = MockAuctionData.GetCategoryNames().ToList();
        model.Conditions = CreateAuctionMockData.Conditions.ToList();
        model.Grades = CreateAuctionMockData.Grades.ToList();
        model.Languages = CreateAuctionMockData.Languages.ToList();
    }

    public void PopulateOptions(CreateBuyNowViewModel model)
    {
        model.Categories = MockAuctionData.GetCategoryNames().ToList();
        model.Conditions = CreateAuctionMockData.Conditions.ToList();
        model.Grades = CreateAuctionMockData.Grades.ToList();
        model.Languages = CreateAuctionMockData.Languages.ToList();
    }

    public IEnumerable<(string Key, string Message)> ValidateCreateAuction(CreateAuctionViewModel model)
    {
        PopulateOptions(model);

        if (model.StartDate < DateTime.Now.AddMinutes(-1))
        {
            yield return (nameof(model.StartDate), "Start date cannot be in the past.");
        }

        if (model.EndDate <= model.StartDate)
        {
            yield return (nameof(model.EndDate), "End date must be greater than start date.");
        }

        if (model.BuyNowPrice.HasValue && model.BuyNowPrice.Value <= model.StartingPrice)
        {
            yield return (nameof(model.BuyNowPrice), "Buy now price must be greater than the starting price.");
        }

        if (model.Year is < 1800 or > 2100)
        {
            yield return (nameof(model.Year), "Please enter a valid year between 1800 and 2100.");
        }
    }

    public IEnumerable<(string Key, string Message)> ValidateCreateBuyNow(CreateBuyNowViewModel model)
    {
        PopulateOptions(model);

        if (model.Price <= 0)
        {
            yield return (nameof(model.Price), "Price must be greater than 0.");
        }
    }
}
