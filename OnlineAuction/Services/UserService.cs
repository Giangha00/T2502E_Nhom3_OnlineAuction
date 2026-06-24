using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using OnlineAuction.Areas.Admin.ViewModels.Users;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Helpers;
using OnlineAuction.Models;
using OnlineAuction.Services.Interfaces;
using AdminUserDetailViewModel = OnlineAuction.Areas.Admin.ViewModels.Users.UserDetailViewModel;
using PublicUserDetailViewModel = OnlineAuction.Models.UserDetailViewModel;

namespace OnlineAuction.Services;

public class UserService : IUserService
{
    private readonly AuctionHouseDbContext _dbContext;
    private readonly IAvatarStorageService _avatarStorageService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISellerAuctionService _sellerAuctionService;

    public UserService(
        AuctionHouseDbContext dbContext,
        IAvatarStorageService avatarStorageService,
        UserManager<ApplicationUser> userManager,
        ISellerAuctionService sellerAuctionService)
    {
        _dbContext = dbContext;
        _avatarStorageService = avatarStorageService;
        _userManager = userManager;
        _sellerAuctionService = sellerAuctionService;
    }

    public async Task<PublicUserDetailViewModel?> GetPublicProfileAsync(int id)
    {
        // User Detail bay gio doc seller tu bang users trong MySQL thay vi mock data.
        var seller = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == id)
            .Select(user => new
            {
                user.Id,
                user.UserName,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.AvatarUrl,
                user.Role,
                user.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (seller is null)
        {
            return null;
        }

        // Lay danh sach tin dang theo loai: dau gia va mua ngay.
        var auctions = await _sellerAuctionService.GetSellerAuctionsAsync(id, ListingTypes.Auction);
        var buyNowListings = await _sellerAuctionService.GetSellerAuctionsAsync(id, ListingTypes.BuyNow);

        var completedAuctions = await _dbContext.Auctions
            .AsNoTracking()
            .CountAsync(auction =>
                auction.Product.SellerId == id &&
                auction.Status == AuctionStatuses.Completed);

        var relatedRows = await _dbContext.Auctions
            .AsNoTracking()
            .Where(auction =>
                auction.Product.SellerId != id &&
                auction.Status == AuctionStatuses.Live &&
                auction.ListingType == ListingTypes.Auction)
            .OrderByDescending(auction => auction.CreatedAt)
            .Take(3)
            .Select(auction => new
            {
                auction.Id,
                auction.StartingPrice,
                auction.CurrentPrice,
                auction.Status,
                auction.EndDate,
                ProductName = auction.Product.Name,
                CategoryName = auction.Product.Category.Name,
                auction.Product.PrimaryImage,
                auction.Product.GradeLabel,
                auction.Product.Condition,
                auction.Product.Year
            })
            .ToListAsync();

        var related = relatedRows
            .Select(auction => new AuctionItemViewModel
            {
                Id = auction.Id,
                Name = auction.ProductName,
                Category = auction.CategoryName,
                ImageUrl = auction.PrimaryImage,
                StartingPrice = auction.StartingPrice,
                CurrentPrice = auction.CurrentPrice,
                Status = auction.Status,
                TimeRemaining = FormatAuctionTimeRemaining(auction.EndDate),
                Grade = auction.GradeLabel ?? string.Empty,
                Condition = auction.Condition,
                Year = auction.Year ?? 0
            })
            .ToList();

        return new PublicUserDetailViewModel
        {
            Profile = new UserProfileViewModel
            {
                Id = seller.Id,
                Username = seller.UserName ?? string.Empty,
                FullName = seller.FullName,
                AvatarUrl = seller.AvatarUrl ?? "/admin/images/user/user-01.jpg",
                Role = seller.Role.ToString(),
                MemberSince = seller.CreatedAt.Year
            },
            BasicInfo = new UserBasicInfoViewModel
            {
                FullName = seller.FullName,
                Email = seller.Email ?? string.Empty,
                PhoneNumber = seller.PhoneNumber ?? string.Empty
            },
            Statistics = new SellerStatisticsViewModel
            {
                TotalAuctions = auctions.Count + buyNowListings.Count,
                CompletedAuctions = completedAuctions,
                TotalSales = completedAuctions,
                Rating = 0
            },
            Auctions = auctions,
            BuyNowListings = buyNowListings,
            Rating = new SellerRatingViewModel
            {
                AverageRating = 0,
                ReviewCount = 0,
                Reviews = []
            },
            RelatedAuctions = related
        };
    }

    public async Task<UserListViewModel> GetUsersAsync(UserFilterViewModel filter)
    {
        NormalizeFilter(filter);

        var query = _dbContext.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var keyword = filter.Search.Trim();

            query = query.Where(user =>
                user.FullName.Contains(keyword) ||
                (user.Email != null && user.Email.Contains(keyword)) ||
                (user.PhoneNumber != null && user.PhoneNumber.Contains(keyword)) ||
                (user.UserName != null && user.UserName.Contains(keyword)));
        }

        if (filter.Role.HasValue)
        {
            query = query.Where(user => user.Role == filter.Role.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(user => user.Status == filter.Status.Value);
        }

        var dateRange = ParseDateRange(filter.DateRange);

        if (dateRange.StartDate.HasValue && dateRange.EndDate.HasValue)
        {
            query = query.Where(user =>
                user.CreatedAt >= dateRange.StartDate.Value &&
                user.CreatedAt < dateRange.EndDate.Value);
        }
        else
        {
            if (filter.FromDate.HasValue)
            {
                query = query.Where(user => user.CreatedAt >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                var toDate = filter.ToDate.Value.Date.AddDays(1);
                query = query.Where(user => user.CreatedAt < toDate);
            }
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)filter.PageSize);

        query = filter.SortOrder switch
        {
            "name_desc" => query.OrderByDescending(user => user.FullName),
            "date_asc" => query.OrderBy(user => user.CreatedAt),
            "date_desc" => query.OrderByDescending(user => user.CreatedAt),
            _ => query.OrderBy(user => user.FullName)
        };

        var users = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(user => new UserListItemViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                Username = user.UserName ?? string.Empty,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role,
                Status = user.Status,
                CreatedAt = user.CreatedAt
            })
            .ToListAsync();

        return new UserListViewModel
        {
            Users = users,
            Filter = filter,
            TotalItems = totalItems,
            TotalPages = totalPages
        };
    }

    public UserFormViewModel BuildCreateForm()
    {
        var model = new UserFormViewModel
        {
            Role = UserRole.User,
            Status = UserStatus.Active
        };

        PopulateOptions(model);

        return model;
    }

    public async Task<UserFormViewModel?> GetEditFormAsync(int id)
    {
        var user = await _dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == id);

        if (user is null)
        {
            return null;
        }

        var model = new UserFormViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            Role = user.Role,
            Status = user.Status,
            CurrentAvatarUrl = user.AvatarUrl
        };

        PopulateOptions(model);

        return model;
    }

    public async Task<AdminUserDetailViewModel?> GetDetailsAsync(int id)
    {
        var user = await _dbContext.Users.AsNoTracking()
            .Where(user => user.Id == id)
            .Select(user => new AdminUserDetailViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Username = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role,
                Status = user.Status,
                AuctionCount = user.Products.Count(),
                HasActiveAuctionOrTransaction =
                    user.Products.Any(p => p.Auctions.Any(a =>
                        a.Status == AuctionStatuses.Live ||
                        a.Status == AuctionStatuses.EndingSoon ||
                        a.Status == AuctionStatuses.AwaitingPayment)) ||
                    user.Orders.Any(o => o.Status == OrderStatuses.PendingPayment),
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            })
            .FirstOrDefaultAsync();

        return user;
    }

    public async Task<(bool Success, string Message)> CreateAsync(UserFormViewModel model)
    {
        var normalizedEmail = model.Email.Trim();
        var normalizedUsername = model.Username.Trim();

        if (await _userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
            return (false, "Email already exists.");
        }

        if (await _userManager.FindByNameAsync(normalizedUsername) is not null)
        {
            return (false, "Username already exists.");
        }

        if (string.IsNullOrWhiteSpace(model.InitialPassword))
        {
            return (false, "Password is required.");
        }

        var avatarUrl = await _avatarStorageService.SaveAvatarAsync(model.AvatarFile);

        var user = new ApplicationUser
        {
            UserName = normalizedUsername,
            Email = normalizedEmail,
            FullName = model.FullName.Trim(),
            PhoneNumber = model.PhoneNumber.Trim(),
            Role = model.Role,
            Status = model.Status,
            AvatarUrl = avatarUrl,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.InitialPassword);

        return result.Succeeded
            ? (true, "User created successfully.")
            : (false, string.Join(" ", result.Errors.Select(error => error.Description)));
    }

    public async Task<(bool Success, string Message)> UpdateAsync(UserFormViewModel model)
    {
        if (!model.Id.HasValue)
        {
            return (false, "Invalid user.");
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(user => user.Id == model.Id.Value);

        if (user is null)
        {
            return (false, "User not found.");
        }

        var normalizedEmail = model.Email.Trim();
        var normalizedUsername = model.Username.Trim();

        var existingEmailUser = await _userManager.FindByEmailAsync(normalizedEmail);
        if (existingEmailUser is not null && existingEmailUser.Id != model.Id.Value)
        {
            return (false, "Email already exists.");
        }

        var existingUsernameUser = await _userManager.FindByNameAsync(normalizedUsername);
        if (existingUsernameUser is not null && existingUsernameUser.Id != model.Id.Value)
        {
            return (false, "Username already exists.");
        }

        var avatarUrl = await _avatarStorageService.SaveAvatarAsync(model.AvatarFile);

        user.FullName = model.FullName.Trim();
        user.UserName = normalizedUsername;
        user.Email = normalizedEmail;
        user.PhoneNumber = model.PhoneNumber.Trim();
        user.Role = model.Role;
        user.Status = model.Status;
        user.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(avatarUrl))
        {
            user.AvatarUrl = avatarUrl;
        }

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return (false, string.Join(" ", updateResult.Errors.Select(error => error.Description)));
        }

        if (!string.IsNullOrWhiteSpace(model.InitialPassword))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.InitialPassword);

            if (!passwordResult.Succeeded)
            {
                return (false, string.Join(" ", passwordResult.Errors.Select(error => error.Description)));
            }
        }

        return (true, "User updated successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == id);

        if (user is null)
        {
            return (false, "User not found.");
        }

        var result = await _userManager.DeleteAsync(user);

        return result.Succeeded
            ? (true, "User deleted successfully.")
            : (false, string.Join(" ", result.Errors.Select(error => error.Description)));
    }

    public async Task<(bool Success, string Message)> ExecuteBulkActionAsync(UserBulkActionViewModel model)
    {
        if (model.SelectedUserIds.Count == 0)
        {
            return (false, "Please select at least one user.");
        }

        var users = await _dbContext.Users
            .Where(user => model.SelectedUserIds.Contains(user.Id))
            .ToListAsync();

        if (users.Count == 0)
        {
            return (false, "No users found.");
        }

        if (model.Action == UserBulkActions.Delete)
        {
            foreach (var user in users)
            {
                var result = await _userManager.DeleteAsync(user);
                if (!result.Succeeded)
                {
                    return (false, string.Join(" ", result.Errors.Select(error => error.Description)));
                }
            }

            return (true, $"Deleted {users.Count} users.");
        }

        if (model.Action == UserBulkActions.ChangeStatus)
        {
            if (!model.Status.HasValue)
            {
                return (false, "Please select a status.");
            }

            foreach (var user in users)
            {
                user.Status = model.Status.Value;
                user.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            return (true, $"Updated status for {users.Count} users.");
        }

        if (model.Action == UserBulkActions.ChangeRole)
        {
            if (!model.Role.HasValue)
            {
                return (false, "Please select a role.");
            }

            foreach (var user in users)
            {
                user.Role = model.Role.Value;
                user.UpdatedAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            return (true, $"Updated role for {users.Count} users.");
        }

        return (false, "Invalid bulk action.");
    }

    private static string FormatAuctionTimeRemaining(DateTime endDate)
    {
        var remaining = DateTimeUtilities.RemainingUtc(endDate);
        if (remaining <= TimeSpan.Zero)
        {
            return "Ended";
        }

        if (remaining.TotalDays >= 1)
        {
            return $"{(int)remaining.TotalDays} days left";
        }

        if (remaining.TotalHours >= 1)
        {
            return $"{(int)remaining.TotalHours} hours left";
        }

        return $"{Math.Max(1, (int)remaining.TotalMinutes)} minutes left";
    }

    private static void NormalizeFilter(UserFilterViewModel filter)
    {
        if (filter.Page <= 0)
        {
            filter.Page = 1;
        }

        filter.PageSize = 10;
    }

    private static void PopulateOptions(UserFormViewModel model)
    {
        model.RoleOptions =
        [
            new SelectListItem("User", UserRole.User.ToString()),
            new SelectListItem("Admin", UserRole.Admin.ToString())
        ];

        model.StatusOptions =
        [
            new SelectListItem("Active", UserStatus.Active.ToString()),
            new SelectListItem("Inactive", UserStatus.Inactive.ToString())
        ];
    }

    private static (DateTime? StartDate, DateTime? EndDate) ParseDateRange(string? dateRange)
    {
        if (string.IsNullOrWhiteSpace(dateRange))
        {
            return (null, null);
        }

        var dates = dateRange.Split(" - ", StringSplitOptions.TrimEntries);

        if (dates.Length != 2)
        {
            return (null, null);
        }

        var isStartValid = DateTime.TryParseExact(
            dates[0],
            "MM/dd/yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var startDate);

        var isEndValid = DateTime.TryParseExact(
            dates[1],
            "MM/dd/yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var endDate);

        if (!isStartValid || !isEndValid)
        {
            return (null, null);
        }

        return (startDate.Date, endDate.Date.AddDays(1));
    }
}
