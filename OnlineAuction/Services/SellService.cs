using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class SellService : ISellService
{
    private readonly AuctionHouseDbContext _db;

    public SellService(AuctionHouseDbContext db)
    {
        _db = db;
    }

    public CreateAuctionViewModel BuildCreateForm()
    {
        var (registrationStart, registrationEnd, liveStart, liveEnd) =
            AuctionScheduleHelper.CreateDefaultSchedule();

        var model = new CreateAuctionViewModel
        {
            RegistrationStartDate = registrationStart,
            RegistrationEndDate = registrationEnd,
            StartDate = liveStart,
            EndDate = liveEnd,
            BidStep = 50,
            Authenticator = "PSA",
            GradeValue = "10",
            Language = "English"
        };

        PopulateOptions(model);
        NormalizeGradingFields(model);
        return model;
    }

    public CreateBuyNowViewModel BuildBuyNowForm()
    {
        var model = new CreateBuyNowViewModel
        {
            Authenticator = "PSA",
            GradeValue = "10",
            Language = "English"
        };

        PopulateOptions(model);
        NormalizeGradingFields(model);
        return model;
    }

    public void PopulateOptions(SellProductFormViewModel model)
    {
        model.Categories = _db.Categories
            .Where(category => category.DeletedAt == null && category.IsActive)
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Name)
            .Select(category => category.Name)
            .ToList();

        model.Authenticators = GradeLabelHelper.Authenticators.ToList();
        model.GradeValues = GradeLabelHelper.GradeValues.ToList();
        model.Languages = CreateAuctionMockData.Languages.ToList();
    }

    public void PopulateOptions(CreateAuctionViewModel model) => PopulateOptions((SellProductFormViewModel)model);

    public void PopulateOptions(CreateBuyNowViewModel model) => PopulateOptions((SellProductFormViewModel)model);

    public IEnumerable<(string Key, string Message)> ValidateCreateAuction(CreateAuctionViewModel model)
    {
        PopulateOptions(model);
        NormalizeGradingFields(model);

        foreach (var error in ValidateSharedProductFields(model))
        {
            yield return error;
        }

        if (model.RegistrationStartDate < DateTime.UtcNow.AddMinutes(-1))
        {
            yield return (nameof(model.RegistrationStartDate), "Registration start cannot be in the past.");
        }

        var scheduleError = AuctionScheduleHelper.ValidateSchedule(
            model.RegistrationStartDate,
            model.RegistrationEndDate,
            model.StartDate,
            model.EndDate);

        if (scheduleError is not null)
        {
            yield return (nameof(model.RegistrationEndDate), scheduleError);
        }

        if (model.BuyNowPrice.HasValue && model.BuyNowPrice.Value <= model.StartingPrice)
        {
            yield return (nameof(model.BuyNowPrice), "Buy now price must be greater than the starting price.");
        }
    }

    public IEnumerable<(string Key, string Message)> ValidateCreateBuyNow(CreateBuyNowViewModel model)
    {
        PopulateOptions(model);
        NormalizeGradingFields(model);

        foreach (var error in ValidateSharedProductFields(model))
        {
            yield return error;
        }

        if (model.Price <= 0.01m)
        {
            yield return (nameof(model.Price), "Price must be greater than 0.01.");
        }
    }

    public static void NormalizeGradingFields(SellProductFormViewModel model)
    {
        model.Grade = GradeLabelHelper.Compose(model.Authenticator, model.GradeValue);
        model.Condition = GradeLabelHelper.ResolveCondition(model.Authenticator);
    }

    private static IEnumerable<(string Key, string Message)> ValidateSharedProductFields(SellProductFormViewModel model)
    {
        if (!model.Year.HasValue)
        {
            yield return (nameof(model.Year), "Year is required.");
        }
        else if (model.Year is < 1800 or > 2100)
        {
            yield return (nameof(model.Year), "Please enter a valid year between 1800 and 2100.");
        }

        if (string.IsNullOrWhiteSpace(model.Authenticator))
        {
            yield return (nameof(model.Authenticator), "Please select an authenticator.");
        }
        else if (!string.Equals(model.Authenticator, GradeLabelHelper.Ungraded, StringComparison.OrdinalIgnoreCase)
                 && string.IsNullOrWhiteSpace(model.GradeValue))
        {
            yield return (nameof(model.GradeValue), "Please select a grade.");
        }
    }
}
