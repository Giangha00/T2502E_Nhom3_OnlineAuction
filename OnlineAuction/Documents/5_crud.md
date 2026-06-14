# CRUD với MYSQL

## 1. Khởi tạo dự án và Cài đặt Thư viện
1.  Khởi tạo dự án **ASP.NET Core Web App (Mvc)** phiên bản .NET 8.
2.  Mở cửa sổ **NuGet**, tìm kiếm và cài đặt hai gói thư viện sau:
    *   `Pomelo.EntityFrameworkCore.MySql`: Thư viện kết nối MySQL.
    *   `Microsoft.EntityFrameworkCore.Design`: Công cụ hỗ trợ khởi tạo cấu trúc dữ liệu.

---

## 2. Cấu hình Chuỗi kết nối (Connection String)
Mở tập tin `appsettings.json` và khai báo thông tin kết nối tới máy chủ MySQL:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=ProductDb;user=root;password="
  }
}
```

---

## 3. Xây dựng Tầng Dữ liệu (Data Layer)

### 3.1. Khởi tạo Model
Tạo tập tin `Models/Product.cs` để định nghĩa cấu trúc bảng:

```csharp
using System.ComponentModel.DataAnnotations;

namespace YourProject.Models;

public class Product
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
    public string Name { get; set; } = string.Empty;

    [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
    public decimal Price { get; set; }

    public string? Description { get; set; }
}
```

### 3.2. Khởi tạo Database Context
Tạo tập tin `Data/AppDbContext.cs` để quản lý việc truy vấn:

```csharp
using Microsoft.EntityFrameworkCore;
using YourProject.Models;

namespace YourProject.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Product> Products { get; set; }
}
```

### 3.3. Đăng ký Dịch vụ và Migration
Tại `Program.cs`, đăng ký DbContext vào hệ thống:

```csharp
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));
```

Thực hiện lệnh sau tại **Terminal** để tạo bảng trong MySQL:
```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## 4. Xây dựng Controller xử lý Logic
Tạo tập tin `Controllers/ProductsController.cs`. Sử dụng Dependency Injection để gọi `AppDbContext`.

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YourProject.Data;
using YourProject.Models;

public class ProductsController : Controller
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context) => _context = context;

    // Danh sách sản phẩm
    public async Task<IActionResult> Index() 
        => View(await _context.Products.ToListAsync());

    // Giao diện Thêm mới
    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(Product product)
    {
        if (!ModelState.IsValid) return View(product);
        _context.Add(product);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // Giao diện Chỉnh sửa
    public async Task<IActionResult> Edit(int id)
    {
        var product = await _context.Products.FindAsync(id);
        return product == null ? NotFound() : View(product);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(Product product)
    {
        if (!ModelState.IsValid) return View(product);
        _context.Update(product);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // Xử lý Xóa
    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }
}
```

---

## 5. Xây dựng giao diện (Views)
Tạo thư mục `Views/Products/` và các tập tin tương ứng.

### 5.1. Trang danh sách (`Index.cshtml`)
```razor
@model IEnumerable<YourProject.Models.Product>

<a asp-action="Create" class="btn btn-primary">Thêm mới</a>
<table class="table">
    <thead>
        <tr>
            <th>Tên</th>
            <th>Giá</th>
            <th>Thao tác</th>
        </tr>
    </thead>
    <tbody>
    @foreach (var item in Model) {
        <tr>
            <td>@item.Name</td>
            <td>@item.Price</td>
            <td>
                <a asp-action="Edit" asp-route-id="@item.Id">Sửa</a> |
                <form asp-action="Delete" asp-route-id="@item.Id" method="post" style="display:inline">
                    <button type="submit" class="btn btn-link text-danger">Xóa</button>
                </form>
            </td>
        </tr>
    }
    </tbody>
</table>
```

### 5.2. Trang thêm mới/chỉnh sửa (`Create.cshtml` & `Edit.cshtml`)
*(Cấu trúc tương tự nhau, dưới đây là ví dụ cho Edit)*
```razor
@model YourProject.Models.Product

<h2>Chỉnh sửa sản phẩm</h2>
<form asp-action="Edit" method="post">
    <input type="hidden" asp-for="Id" />
    <div class="form-group">
        <label asp-for="Name"></label>
        <input asp-for="Name" class="form-control" />
        <span asp-validation-for="Name" class="text-danger"></span>
    </div>
    <div class="form-group">
        <label asp-for="Price"></label>
        <input asp-for="Price" class="form-control" />
        <span asp-validation-for="Price" class="text-danger"></span>
    </div>
    <button type="submit" class="btn btn-success">Cập nhật</button>
    <a asp-action="Index">Hủy</a>
</form>
```

---

## 6. Tổng kết
Quy trình trên đã thiết lập một hệ thống CRUD hoàn chỉnh:
1.  **Dữ liệu:** Quản lý bởi Entity Framework Core thông qua MySQL Provider.
2.  **Logic:** Xử lý bất đồng bộ (Async/Await) trong Controller nhằm tối ưu hiệu suất.
3.  **Giao diện:** Sử dụng Tag Helpers để liên kết chặt chẽ với Model, đảm bảo tính nhất quán của dữ liệu từ Client lên Server.

Việc thực hiện thủ công giúp lập trình viên kiểm soát hoàn toàn mã nguồn, dễ dàng tùy biến logic nghiệp vụ và tối ưu hóa các truy vấn cơ sở dữ liệu khi cần thiết.