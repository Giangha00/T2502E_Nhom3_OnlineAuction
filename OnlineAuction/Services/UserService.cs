using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using OnlineAuction.Areas.Admin.ViewModels.Users;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
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

    public UserService(
        AuctionHouseDbContext dbContext,
        IAvatarStorageService avatarStorageService,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _avatarStorageService = avatarStorageService;
        _userManager = userManager;
    }

    public Task<PublicUserDetailViewModel?> GetPublicProfileAsync(int id) =>
        Task.FromResult(BuildPublicProfile(id));

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
                AuctionCount = user.Products.Count,
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

    private static PublicUserDetailViewModel? BuildPublicProfile(int id)
    {
        var seller = MockAuctionData.GetBestSellers().FirstOrDefault(s => s.Id == id);
        if (seller is null)
        {
            return null;
        }

        var profileExtras = GetProfileExtras(id);
        var auctions = MockAuctionData.GetAuctionsBySellerId(id);
        var related = MockAuctionData.GetAllAuctions()
            .Where(a => !auctions.Any(x => x.Id == a.Id))
            .Take(3)
            .ToList();

        return new PublicUserDetailViewModel
        {
            Profile = new UserProfileViewModel
            {
                Id = seller.Id,
                Username = seller.Username,
                FullName = profileExtras.FullName,
                AvatarUrl = seller.AvatarUrl,
                Role = "Seller",
                MemberSince = profileExtras.MemberSince
            },
            BasicInfo = new UserBasicInfoViewModel
            {
                FullName = profileExtras.FullName,
                Email = profileExtras.Email,
                PhoneNumber = profileExtras.Phone,
                Address = profileExtras.Address
            },
            Statistics = new SellerStatisticsViewModel
            {
                TotalAuctions = seller.AuctionCount,
                CompletedAuctions = seller.SuccessfulSales,
                TotalSales = profileExtras.TotalSales,
                Rating = seller.Rating
            },
            Auctions = auctions,
            Rating = new SellerRatingViewModel
            {
                AverageRating = seller.Rating,
                ReviewCount = profileExtras.ReviewCount,
                Reviews = GetReviewsForSeller(id)
            },
            RelatedAuctions = related
        };
    }

    private static (string FullName, string Email, string Phone, string Address, int MemberSince, int TotalSales, int ReviewCount) GetProfileExtras(int id) =>
        id switch
        {
            1 => ("Elena Voss", "elena.voss@gmail.com", "+84 912 345 678", "Ha Noi, Viet Nam", 2022, 120, 98),
            2 => ("Marcus Chen", "marcus.chen@gmail.com", "+84 987 654 321", "Ho Chi Minh, Viet Nam", 2023, 95, 76),
            3 => ("Sofia Nguyen", "sofia.gallery@gmail.com", "+84 901 234 567", "Da Nang, Viet Nam", 2021, 156, 142),
            4 => ("James Retro", "james.retro@gmail.com", "+84 933 221 100", "Ha Noi, Viet Nam", 2024, 88, 54),
            _ => ("John Smith", "john@gmail.com", "+84 xxx xxx xxx", "Ha Noi, Viet Nam", 2026, 120, 120)
        };

    private static List<SellerReviewViewModel> GetReviewsForSeller(int id) =>
        id switch
        {
            1 =>
            [
                new() { ReviewerName = "Michael", Rating = 5, Comment = "Great seller! Fast shipping and item exactly as described.", ReviewDate = new DateTime(2026, 6, 10) },
                new() { ReviewerName = "Anna", Rating = 5, Comment = "Professional communication throughout the auction.", ReviewDate = new DateTime(2026, 5, 28) },
                new() { ReviewerName = "David", Rating = 4.5, Comment = "Smooth transaction. Would buy again.", ReviewDate = new DateTime(2026, 5, 12) }
            ],
            2 =>
            [
                new() { ReviewerName = "Michael", Rating = 5, Comment = "Great seller!", ReviewDate = new DateTime(2026, 6, 10) },
                new() { ReviewerName = "Lisa", Rating = 4.5, Comment = "Reliable seller with quality items.", ReviewDate = new DateTime(2026, 4, 20) }
            ],
            3 =>
            [
                new() { ReviewerName = "Tom", Rating = 5, Comment = "Outstanding gallery pieces and packaging.", ReviewDate = new DateTime(2026, 6, 8) },
                new() { ReviewerName = "Sarah", Rating = 5, Comment = "Best seller on the platform!", ReviewDate = new DateTime(2026, 5, 30) },
                new() { ReviewerName = "Kevin", Rating = 5, Comment = "Highly recommend for art collectors.", ReviewDate = new DateTime(2026, 5, 15) }
            ],
            4 =>
            [
                new() { ReviewerName = "Michael", Rating = 4, Comment = "Good vintage finds. Delivery took a bit longer.", ReviewDate = new DateTime(2026, 6, 2) },
                new() { ReviewerName = "Emma", Rating = 4.5, Comment = "Authentic retro items as advertised.", ReviewDate = new DateTime(2026, 4, 8) }
            ],
            _ =>
            [
                new() { ReviewerName = "Michael", Rating = 5, Comment = "Great seller!", ReviewDate = new DateTime(2026, 6, 10) }
            ]
        };

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
