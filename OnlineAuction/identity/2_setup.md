# Thiết lập Identity trong dự án .NET 8 MVC sử dụng MySQL.

---

### Bước 1: Cài đặt các thư viện cần thiết (NuGet)

Mở Terminal và cài đặt gói Identity dành cho Entity Framework Core:

```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
```
*(Gói Pomelo MySQL bạn đã cài ở các phần trước là đủ để chạy cùng Identity).*

---

### Bước 2: Tạo lớp AppUser tùy chỉnh (Custom User)

Thay vì dùng lớp `IdentityUser` mặc định, bạn nên tạo lớp riêng để có thể thêm các trường như Họ tên, Địa chỉ, Ảnh đại diện...

```csharp
using Microsoft.AspNetCore.Identity;

namespace YourProject.Models;

public class AppUser : IdentityUser
{
    // Thêm các trường tùy chỉnh
    public string FullName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
```

---

### Bước 3: Cấu hình AppDbContext

Lớp `AppDbContext` bây giờ không kế thừa từ `DbContext` thông thường mà phải kế thừa từ **`IdentityDbContext<AppUser>`**.

```csharp
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using YourProject.Models;

namespace YourProject.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Bạn có thể tùy chỉnh tên các bảng Identity tại đây nếu muốn
        // Ví dụ: builder.Entity<AppUser>().ToTable("Users");
    }
}
```

---

### Bước 4: Đăng ký Identity trong Program.cs

Đây là bước quan trọng nhất để hệ thống nhận diện các dịch vụ xác thực.

```csharp
using Microsoft.AspNetCore.Identity;
using YourProject.Data;
using YourProject.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình DbContext (như cũ)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 2. Đăng ký Dịch vụ Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options => {
    // Cấu hình độ khó mật khẩu
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    
    // Cấu hình khóa tài khoản khi nhập sai nhiều lần
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    
    // Yêu cầu Email duy nhất
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// 3. Cấu hình Cookie (Thời hạn đăng nhập, đường dẫn chuyển hướng)
builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
});

builder.Services.AddControllersWithViews();

var app = builder.Build();

// 4. Kích hoạt Middleware Xác thực (Bắt buộc phải nằm TRƯỚC UseAuthorization)
app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
```

---

### Bước 5: Cập nhật Database (Migration)

Identity sẽ tạo ra khoảng 7 bảng mới (`AspNetUsers`, `AspNetRoles`,...). Hãy chạy lệnh sau:

```bash
dotnet ef migrations add AddIdentityTables
dotnet ef database update
```

---

### Bước 6: Sử dụng Identity trong Controller (Ví dụ Đăng ký/Đăng nhập)

Bạn sẽ sử dụng `UserManager` để quản lý User và `SignInManager` để xử lý đăng nhập.

```csharp
public class AccountController : Controller
{
    private readonly UserManager<AppUser> _userManager;
    private readonly SignInManager<AppUser> _signInManager;

    public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // Đăng ký (Register)
    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var user = new AppUser { 
                UserName = model.Email, 
                Email = model.Email, 
                FullName = model.FullName 
            };
            
            var result = await _userManager.CreateAsync(user, model.Password);
            
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return RedirectToAction("Index", "Home");
            }
            
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
        }
        return View(model);
    }

    // Đăng xuất (Logout)
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }
}
```

---

### Bước 7: Kiểm tra trạng thái đăng nhập trong View

Bạn có thể inject `SignInManager` và `UserManager` trực tiếp vào View để hiển thị thông tin người dùng.

```razor
@using Microsoft.AspNetCore.Identity
@inject SignInManager<AppUser> SignInManager
@inject UserManager<AppUser> UserManager

@if (SignInManager.IsSignedIn(User))
{
    <li class="nav-item">
        <a class="nav-link">Xin chào @UserManager.GetUserName(User)!</a>
    </li>
    <li class="nav-item">
        <a class="nav-link" asp-controller="Account" asp-action="Logout">Đăng xuất</a>
    </li>
}
else
{
    <li class="nav-item">
        <a class="nav-link" asp-controller="Account" asp-action="Login">Đăng nhập</a>
    </li>
}
```

### Tổng kết:
Bằng cách này, bạn đã tự tay thiết lập hệ thống bảo mật tiêu chuẩn của Microsoft:
1.  **Model:** Kế thừa `IdentityUser`.
2.  **Context:** Kế thừa `IdentityDbContext`.
3.  **Program.cs:** Cấu hình Authentication Middleware.
4.  **Logic:** Dùng `UserManager` và `SignInManager` để điều khiển.

Bạn có muốn tôi hướng dẫn chi tiết về cách **Phân quyền (Roles)** như Admin/User sau khi đã setup xong phần này không?