# Hướng dẫn chi tiết về quản lý quyền và kiểm tra quyền truy cập.

---

### 1. Tạo quyền mới (Role) và Gán quyền cho tài khoản

Thông thường, việc tạo Role có thể thực hiện qua một giao diện quản lý hoặc thông qua **Data Seeding** khi ứng dụng khởi chạy. Chúng ta sẽ sử dụng dịch vụ `RoleManager<IdentityRole>` và `UserManager<AppUser>`.

#### 1.1. Khởi tạo Role tự động (Seeding Roles)
Mở file `Data/DbInitializer.cs` (hoặc nơi bạn viết Seeder) để khởi tạo các quyền cơ bản như "Admin" và "User".

```csharp
public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
{
    var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

    // 1. Tạo các Role nếu chưa tồn tại
    string[] roleNames = { "Admin", "Manager", "User" };
    foreach (var roleName in roleNames)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }

    // 2. Tạo một tài khoản Admin mặc định và gán quyền
    var adminEmail = "admin@system.com";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);

    if (adminUser == null)
    {
        var user = new AppUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "System Administrator",
            EmailConfirmed = true
        };

        var createPowerUser = await userManager.CreateAsync(user, "Admin@123");
        if (createPowerUser.Succeeded)
        {
            // Gán quyền Admin cho tài khoản này
            await userManager.AddToRoleAsync(user, "Admin");
        }
    }
}
```
*Đừng quên gọi phương thức này trong `Program.cs` khi ứng dụng khởi chạy.*

---

### 2. Thêm quyền cho một tài khoản cụ thể (Runtime)

Nếu bạn muốn tạo một giao diện để gán quyền cho người dùng, bạn sẽ sử dụng phương thức `AddToRoleAsync` trong Controller.

```csharp
[HttpPost]
public async Task<IActionResult> AssignRole(string userId, string roleName)
{
    var user = await _userManager.FindByIdAsync(userId);
    if (user != null)
    {
        // Kiểm tra xem Role có tồn tại không
        if (await _roleManager.RoleExistsAsync(roleName))
        {
            await _userManager.AddToRoleAsync(user, roleName);
            return Ok($"Đã gán quyền {roleName} cho người dùng {user.FullName}");
        }
    }
    return NotFound();
}
```

---

### 3. Kiểm tra quyền truy cập theo Path (Authorization)

Để kiểm tra quyền và chặn người dùng không đủ thẩm quyền truy cập vào các Path cụ thể, bạn sử dụng thuộc tính `[Authorize]`.

#### 3.1. Chặn theo Controller (Tất cả các Action bên trong)
```csharp
[Authorize(Roles = "Admin")] // Chỉ tài khoản có Role Admin mới truy cập được /Admin/...
public class AdminController : Controller
{
    public IActionResult Index() => View();
}
```

#### 3.2. Chặn theo từng Action cụ thể
```csharp
public class CourseController : Controller
{
    public IActionResult Index() => View(); // Ai cũng xem được danh sách

    [Authorize(Roles = "Admin,Manager")] // Chỉ Admin hoặc Manager mới được tạo khóa học
    public IActionResult Create() => View();
}
```

#### 3.3. Kiểm tra quyền ngay trong logic code
Đôi khi bạn cần kiểm tra quyền ngay giữa một hàm để xử lý logic:
```csharp
public async Task<IActionResult> Delete(int id)
{
    if (User.IsInRole("Admin"))
    {
        // Cho phép xóa bất kỳ ai
    }
    else
    {
        // Chỉ cho phép xóa nếu là chủ sở hữu
    }
    return View();
}
```

---

### 4. Kiểm tra quyền hiển thị trên giao diện (Views)

Sử dụng `@User.IsInRole()` để ẩn/hiện các thành phần giao diện (như nút Xóa, Menu Admin).

```razor
@if (User.Identity.IsAuthenticated)
{
    <li>Xin chào @User.Identity.Name</li>

    @if (User.IsInRole("Admin"))
    {
        <li class="nav-item">
            <a class="nav-link text-danger" asp-controller="Admin" asp-action="Index">QUẢN TRỊ HỆ THỐNG</a>
        </li>
    }
}
```

---

### 5. Cấu hình trang "Từ chối truy cập" (Access Denied)

Nếu một người dùng đã đăng nhập (Role "User") nhưng cố tình truy cập vào Path dành cho "Admin", Identity sẽ chuyển hướng họ đến trang `AccessDenied`.

Cấu hình trong `Program.cs`:
```csharp
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied"; // Đường dẫn tới trang thông báo lỗi quyền
});
```

Tạo một Action trong `AccountController`:
```csharp
public IActionResult AccessDenied()
{
    return View(); // Trả về giao diện thông báo: "Bạn không có quyền truy cập trang này"
}
```

---

### Tổng kết quy trình phân quyền:
1.  **Dịch vụ:** Sử dụng `RoleManager` để tạo quyền, `UserManager` để gán quyền.
2.  **Middleware:** Đảm bảo `app.UseAuthentication()` và `app.UseAuthorization()` được gọi theo đúng thứ tự trong `Program.cs`.
3.  **Attribute:** Sử dụng `[Authorize(Roles = "Name")]` trên Controller/Action để bảo vệ các đường dẫn (Path).
4.  **UI:** Sử dụng `User.IsInRole("Name")` để tùy biến hiển thị cho người dùng.
