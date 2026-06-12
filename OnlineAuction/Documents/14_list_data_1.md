Chức năng hiển thị danh sách người dùng (User) trong bảng sử dụng **Bootstrap 5** trong dự án .NET 8 MVC.

---

### Bước 1: Tạo Model
Tạo lớp `User` trong thư mục `Models/`. Đây là thực thể đại diện cho bảng người dùng trong Database.

```csharp
namespace YourProject.Models;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
```
*Đừng quên thêm `DbSet<User> Users { get; set; }` vào `AppDbContext.cs`.*

---

### Bước 2: Tạo Migration và Update Database
Mở Terminal và thực thi lệnh để EF Core tạo bảng trong MySQL:

```bash
dotnet ef migrations add AddUserTable
dotnet ef database update
```

---

### Bước 3: Tạo Seeder (Khởi tạo dữ liệu mẫu)
Sử dụng phương pháp `DbInitializer` để đảm bảo có dữ liệu hiển thị ngay khi chạy ứng dụng.

*Trong `Data/DbInitializer.cs`:*

```csharp
public static void SeedUsers(AppDbContext context)
{
    if (!context.Users.Any())
    {
        context.Users.AddRange(new List<User>
        {
            new User { FullName = "Nguyễn Văn A", Email = "a@gmail.com", Role = "Admin" },
            new User { FullName = "Trần Thị B", Email = "b@gmail.com", Role = "Member" },
            new User { FullName = "Lê Văn C", Email = "c@gmail.com", Role = "Member" }
        });
        context.SaveChanges();
    }
}
```
*Gọi `DbInitializer.SeedUsers(context);` trong `Program.cs`.*

---

### Bước 4: Tạo Controller
Xử lý logic lấy danh sách User từ Database và truyền sang View.

```csharp
public class UserController : Controller
{
    private readonly AppDbContext _context;
    public UserController(AppDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var users = await _context.Users.ToListAsync();
        return View(users);
    }
}
```

---

### Bước 5: Hiển thị ra View với Bootstrap 5
Tạo file `Views/User/Index.cshtml`. Sử dụng các class `table`, `table-striped`, `table-hover` của Bootstrap để bảng trông chuyên nghiệp.

```razor
@model IEnumerable<YourProject.Models.User>

<div class="container mt-4">
    <h2 class="mb-4">Danh sách người dùng</h2>

    <table class="table table-striped table-hover table-bordered">
        <thead class="table-dark">
            <tr>
                <th>ID</th>
                <th>Họ tên</th>
                <th>Email</th>
                <th>Vai trò</th>
                <th>Ngày tạo</th>
            </tr>
        </thead>
        <tbody>
            @foreach (var user in Model)
            {
                <tr>
                    <td>@user.Id</td>
                    <td>@user.FullName</td>
                    <td>@user.Email</td>
                    <td>
                        @if(user.Role == "Admin") {
                            <span class="badge bg-danger">@user.Role</span>
                        } else {
                            <span class="badge bg-primary">@user.Role</span>
                        }
                    </td>
                    <td>@user.CreatedAt.ToString("dd/MM/yyyy")</td>
                </tr>
            }
        </tbody>
    </table>
</div>
```

---

### Các lưu ý kỹ thuật:
1.  **Bootstrap:** Đảm bảo bạn đã nhúng file CSS của Bootstrap 5 trong `Views/Shared/_Layout.cshtml` (thông qua CDN hoặc Libman).
2.  **Định dạng:** Sử dụng `ToString("dd/MM/yyyy")` để hiển thị ngày tháng theo chuẩn Việt Nam thay vì định dạng mặc định của hệ thống.
3.  **Badge:** Sử dụng class `badge` của Bootstrap để làm nổi bật các trạng thái như "Admin" (màu đỏ) và "Member" (màu xanh), giúp giao diện trực quan hơn.
4.  **Table responsiveness:** Nếu bảng có quá nhiều cột, bạn nên bọc thẻ `<table>` trong thẻ `div` có class `table-responsive` để bảng không bị vỡ giao diện trên điện thoại.