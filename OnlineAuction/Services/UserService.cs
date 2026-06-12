using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using OnlineAuction.Areas.Admin.ViewModels.Users;
using OnlineAuction.Data;
using OnlineAuction.Entities;
using OnlineAuction.Enums;
using OnlineAuction.Services.Interfaces;

namespace OnlineAuction.Services;

public class UserService : IUserService
{
    private readonly AuctionHouseDbContext _dbContext;
    private readonly IAvatarStorageService _avatarStorageService;

    public UserService(
        AuctionHouseDbContext dbContext,
        IAvatarStorageService avatarStorageService)
    {
        _dbContext = dbContext;
        _avatarStorageService = avatarStorageService;
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
                user.Email.Contains(keyword) ||
                user.PhoneNumber.Contains(keyword));
        }

        if (filter.Role.HasValue)
        {
            query = query.Where(user => user.Role == filter.Role.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(user => user.Status == filter.Status.Value);
        }

        if (filter.Gender.HasValue)
        {
            query = query.Where(user => user.Gender == filter.Gender.Value);
        }

        var dateRange = ParseDateRange(filter.DateRange);

        if (dateRange.StartDate.HasValue && dateRange.EndDate.HasValue)
        {
            query = query.Where(user =>
                user.CreatedDate >= dateRange.StartDate.Value &&
                user.CreatedDate < dateRange.EndDate.Value);
        }
        else
        {
            if (filter.FromDate.HasValue)
            {
                query = query.Where(user => user.CreatedDate >= filter.FromDate.Value);
            }

            if (filter.ToDate.HasValue)
            {
                var toDate = filter.ToDate.Value.Date.AddDays(1);
                query = query.Where(user => user.CreatedDate < toDate);
            }
        }

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)filter.PageSize);

        query = filter.SortOrder switch
        {
            "name_desc" => query.OrderByDescending(user => user.FullName),
            "date_asc" => query.OrderBy(user => user.CreatedDate),
            "date_desc" => query.OrderByDescending(user => user.CreatedDate),
            _ => query.OrderBy(user => user.FullName)
        };

        var users = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(user => new UserListItemViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role,
                Status = user.Status,
                Gender = user.Gender,
                CreatedDate = user.CreatedDate
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
            Status = UserStatus.Active,
            Gender = Gender.Male
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
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            Status = user.Status,
            Gender = user.Gender,
            CurrentAvatarUrl = user.AvatarUrl
        };

        PopulateOptions(model);

        return model;
    }

    public async Task<UserDetailViewModel?> GetDetailsAsync(int id)
    {
        return await _dbContext.Users.AsNoTracking()
            .Where(user => user.Id == id)
            .Select(user => new UserDetailViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                AvatarUrl = user.AvatarUrl,
                Role = user.Role,
                Status = user.Status,
                Gender = user.Gender,
                AuctionCount = user.AuctionCount,
                HasActiveAuctionOrTransaction = user.HasActiveAuctionOrTransaction,
                CreatedDate = user.CreatedDate,
                UpdatedDate = user.UpdatedDate
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message)> CreateAsync(UserFormViewModel model)
    {
        var normalizedEmail = model.Email.Trim();

        var isEmailExists = await _dbContext.Users
            .AnyAsync(user => user.Email == normalizedEmail);

        if (isEmailExists)
        {
            return (false, "Email already exists.");
        }

        if (string.IsNullOrWhiteSpace(model.InitialPassword))
        {
            return (false, "Password is required.");
        }

        var avatarUrl = await _avatarStorageService.SaveAvatarAsync(model.AvatarFile);

        var user = new User
        {
            FullName = model.FullName.Trim(),
            Email = normalizedEmail,
            PhoneNumber = model.PhoneNumber.Trim(),
            Role = model.Role,
            Status = model.Status,
            Gender = model.Gender,
            AvatarUrl = avatarUrl,
            InitialPassword = model.InitialPassword,
            CreatedDate = DateTime.UtcNow
        };

        await _dbContext.Users.AddAsync(user);
        await _dbContext.SaveChangesAsync();

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

        var isEmailExists = await _dbContext.Users
            .AnyAsync(otherUser =>
                otherUser.Email == normalizedEmail &&
                otherUser.Id != model.Id.Value);

        if (isEmailExists)
        {
            return (false, "Email already exists.");
        }

        var avatarUrl = await _avatarStorageService.SaveAvatarAsync(model.AvatarFile);

        user.FullName = model.FullName.Trim();
        user.Email = normalizedEmail;
        user.PhoneNumber = model.PhoneNumber.Trim();
        user.Role = model.Role;
        user.Status = model.Status;
        user.Gender = model.Gender;
        user.UpdatedDate = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(model.InitialPassword))
        {
            user.InitialPassword = model.InitialPassword;
        }

        if (!string.IsNullOrWhiteSpace(avatarUrl))
        {
            user.AvatarUrl = avatarUrl;
        }

        await _dbContext.SaveChangesAsync();

        return (true, "User updated successfully.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(user => user.Id == id);

        if (user is null)
        {
            return (false, "User not found.");
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync();

        return (true, "User deleted successfully.");
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
            _dbContext.Users.RemoveRange(users);
            await _dbContext.SaveChangesAsync();

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
                user.UpdatedDate = DateTime.UtcNow;
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
                user.UpdatedDate = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            return (true, $"Updated role for {users.Count} users.");
        }

        return (false, "Invalid bulk action.");
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
            new SelectListItem("Inactive", UserStatus.Inactive.ToString()),
            new SelectListItem("Blocked", UserStatus.Blocked.ToString())
        ];

        model.GenderOptions =
        [
            new SelectListItem("Male", Gender.Male.ToString()),
            new SelectListItem("Female", Gender.Female.ToString())
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
