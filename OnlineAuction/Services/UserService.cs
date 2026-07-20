using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using OnlineAuction.Areas.Admin.ViewModels.Users;
using OnlineAuction.Data;
using OnlineAuction.Data.Seeders;
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
    private readonly IPermissionService _permissionService;

    public UserService(
        AuctionHouseDbContext dbContext,
        IAvatarStorageService avatarStorageService,
        UserManager<ApplicationUser> userManager,
        ISellerAuctionService sellerAuctionService,
        IPermissionService permissionService)
    {
        _dbContext = dbContext;
        _avatarStorageService = avatarStorageService;
        _userManager = userManager;
        _sellerAuctionService = sellerAuctionService;
        _permissionService = permissionService;
    }

    public async Task<PublicUserDetailViewModel?> GetPublicProfileAsync(int id, int? viewerUserId = null)
    {
        var isOwner = viewerUserId.HasValue && viewerUserId.Value == id;

        // Public profile: active, non-deleted users only. Owner can still open own inactive account.
        var seller = await _dbContext.Users
            .AsNoTracking()
            .Where(user =>
                user.Id == id &&
                user.DeletedAt == null &&
                (isOwner || user.Status == UserStatus.Active))
            .Select(user => new
            {
                user.Id,
                user.UserName,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.AvatarUrl,
                user.Role,
                user.EmailConfirmed,
                user.CreatedAt
            })
            .FirstOrDefaultAsync();

        if (seller is null)
        {
            return null;
        }

        var auctions = await _sellerAuctionService.GetSellerAuctionsAsync(
            id,
            ListingTypes.Auction,
            forPublicProfile: true,
            includeOwnerDrafts: isOwner);
        var buyNowListings = await _sellerAuctionService.GetSellerAuctionsAsync(
            id,
            ListingTypes.BuyNow,
            forPublicProfile: true,
            includeOwnerDrafts: isOwner);

        // Keep completed / history scoped to auction listings so success rate uses one denominator.
        var completedAuctions = await _dbContext.Auctions
            .AsNoTracking()
            .CountAsync(auction =>
                auction.Product.SellerId == id &&
                auction.ListingType == ListingTypes.Auction &&
                auction.Status == AuctionStatuses.Completed);

        var totalAuctionHistory = await _dbContext.Auctions
            .AsNoTracking()
            .CountAsync(auction =>
                auction.Product.SellerId == id &&
                auction.ListingType == ListingTypes.Auction &&
                auction.Status != AuctionStatuses.Cancelled);

        var paidOrderEarnings = await _dbContext.OrderItems
            .AsNoTracking()
            .Where(item =>
                item.DeletedAt == null &&
                item.Order.DeletedAt == null &&
                item.Auction.Product.SellerId == id &&
                (item.Order.Status == OrderStatuses.Paid || item.Order.Status == OrderStatuses.Delivered))
            .Select(item => new
            {
                item.OrderId,
                item.Order.Subtotal,
                item.Order.SellerFee,
                item.Order.SellerProceeds
            })
            .ToListAsync();

        var uniquePaidOrders = paidOrderEarnings
            .GroupBy(row => row.OrderId)
            .Select(group => group.First())
            .ToList();

        var grossSales = uniquePaidOrders.Sum(row => row.Subtotal);
        var sellerFees = uniquePaidOrders.Sum(row => row.SellerFee);
        var netProceeds = uniquePaidOrders.Sum(row => row.SellerProceeds);

        var relatedRows = await _dbContext.Auctions
            .AsNoTracking()
            .Where(auction =>
                auction.Product.SellerId != id &&
                auction.Product.DeletedAt == null &&
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
            IsOwner = isOwner,
            Profile = new UserProfileViewModel
            {
                Id = seller.Id,
                IsOwner = isOwner,
                Username = seller.UserName ?? string.Empty,
                FullName = seller.FullName,
                AvatarUrl = seller.AvatarUrl ?? "/admin/images/user/user-01.jpg",
                Role = seller.Role.ToString(),
                EmailConfirmed = seller.EmailConfirmed,
                MemberSince = seller.CreatedAt.Year
            },
            BasicInfo = new UserBasicInfoViewModel
            {
                FullName = seller.FullName,
                CanViewContactInfo = isOwner,
                Email = isOwner ? (seller.Email ?? string.Empty) : string.Empty,
                PhoneNumber = isOwner ? (seller.PhoneNumber ?? string.Empty) : string.Empty
            },
            Statistics = new SellerStatisticsViewModel
            {
                TotalListings = auctions.Count + buyNowListings.Count,
                TotalAuctions = totalAuctionHistory,
                TotalBuyNowListings = buyNowListings.Count,
                CompletedAuctions = completedAuctions,
                TotalSales = uniquePaidOrders.Count,
                GrossSales = isOwner ? grossSales : 0m,
                SellerFees = isOwner ? sellerFees : 0m,
                NetProceeds = isOwner ? netProceeds : 0m
            },
            Auctions = auctions,
            BuyNowListings = buyNowListings,
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

    public async Task<UserFormViewModel> BuildCreateFormAsync()
    {
        var model = new UserFormViewModel
        {
            Role = UserRole.User,
            Status = UserStatus.Active
        };

        PopulateOptions(model);
        await PopulatePermissionOptionsAsync(model);

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
        await PopulatePermissionOptionsAsync(model, user.Id);

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
                AuctionCount = user.Products.Count(p => p.DeletedAt == null),
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
            EmailConfirmed = true,
            AvatarUrl = avatarUrl,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, model.InitialPassword);

        if (!result.Succeeded)
        {
            return (false, string.Join(" ", result.Errors.Select(error => error.Description)));
        }

        await IdentityRoleSyncService.SyncUserRoleAsync(_userManager, user, model.Role);

        if (model.Role == UserRole.User)
        {
            await _permissionService.UpdateUserPermissionsAsync(user.Id, model.AssignedPermissionIds);
        }
        else
        {
            await _permissionService.UpdateUserPermissionsAsync(user.Id, []);
        }

        return (true, "User created successfully.");
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

        await IdentityRoleSyncService.SyncUserRoleAsync(_userManager, user, model.Role);

        if (model.Role == UserRole.User)
        {
            await _permissionService.UpdateUserPermissionsAsync(user.Id, model.AssignedPermissionIds);
        }
        else
        {
            await _permissionService.UpdateUserPermissionsAsync(user.Id, []);
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
                await IdentityRoleSyncService.SyncUserRoleAsync(_userManager, user, model.Role.Value);
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

    private async Task PopulatePermissionOptionsAsync(UserFormViewModel model, int? userId = null)
    {
        model.AvailablePermissions = (await _permissionService.GetPermissionCatalogAsync()).ToList();
        model.AssignedPermissionIds = userId.HasValue
            ? (await _permissionService.GetAssignedPermissionIdsForUserAsync(userId.Value)).ToList()
            : [];
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
