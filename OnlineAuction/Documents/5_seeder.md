# TRIỂN KHAI DBINITIALIZER VỚI CƠ CHẾ RESET DỮ LIỆU

## 1. Xây dựng lớp DbInitializer chuyên sâu

Tại lớp này, chúng ta sẽ thực hiện hai tác vụ chính: Xóa toàn bộ bản ghi hiện có và khởi tạo lại dữ liệu mới.

**Lưu ý quan trọng:** Thao tác xóa dữ liệu rất nguy hiểm. Chúng ta phải đảm bảo logic này chỉ chạy trong môi trường **Development** (Phát triển).

*Tập tin: `Data/DbInitializer.cs`*

```csharp
using Microsoft.EntityFrameworkCore;
using YourProject.Models;

namespace YourProject.Data;

public static class DbInitializer
{
    public static void Seed(IApplicationBuilder applicationBuilder)
    {
        using (var serviceScope = applicationBuilder.ApplicationServices.CreateScope())
        {
            var context = serviceScope.ServiceProvider.GetRequiredService<AppDbContext>();

            // Đảm bảo Database đã được tạo thông qua Migrations
            context.Database.Migrate();

            // 1. Thực hiện Reset dữ liệu
            ClearData(context);

            // 2. Thực hiện Seeding dữ liệu mới
            SeedData(context);
        }
    }

    private static void ClearData(AppDbContext context)
    {
        // Cách 1: Sử dụng EF Core (An toàn cho mọi Database)
        // Lưu ý: Nếu có khóa ngoại (FK), phải xóa theo thứ tự bảng con trước, bảng cha sau.
        if (context.Products.Any())
        {
            context.Products.RemoveRange(context.Products);
            context.SaveChanges();
        }

        // Cách 2: Sử dụng SQL thuần (Tối ưu cho MySQL - Reset cả Identity/Auto Increment)
        // context.Database.ExecuteSqlRaw("SET FOREIGN_KEY_CHECKS = 0;");
        // context.Database.ExecuteSqlRaw("TRUNCATE TABLE Products;");
        // context.Database.ExecuteSqlRaw("SET FOREIGN_KEY_CHECKS = 1;");
    }

    private static void SeedData(AppDbContext context)
    {
        if (!context.Products.Any())
        {
            context.Products.AddRange(new List<Product>
            {
                new Product { Name = "Màn hình Dell UltraSharp", Price = 500, Description = "27 inch 4K" },
                new Product { Name = "Bàn phím cơ Keychron", Price = 150, Description = "Wireless Mechanical Keyboard" },
                new Product { Name = "Chuột Logitech MX Master 3", Price = 100, Description = "Advanced Wireless Mouse" }
            });

            context.SaveChanges();
        }
    }
}
```

---

## 2. Tích hợp vào Program.cs với điều kiện môi trường

Việc gọi Seeder cần được đặt trong khối kiểm tra môi trường để tránh thảm họa mất dữ liệu trên môi trường Production (Thực tế).

*Tập tin: `Program.cs`*

```csharp
var app = builder.Build();

// Chỉ thực hiện Seeding nếu đang ở môi trường Development
if (app.Environment.IsDevelopment())
{
    // Gọi DbInitializer
    DbInitializer.Seed(app);
}

// ... Các cấu hình Middleware khác
app.UseStaticFiles();
app.UseRouting();
// ...

app.Run();
```

---

## 3. Phân tích các kỹ thuật Reset dữ liệu

### A. Sử dụng `RemoveRange` (Khuyên dùng)
*   **Cơ chế:** EF Core sẽ tạo các lệnh `DELETE` gửi đến MySQL.
*   **Ưu điểm:** Hoạt động tốt với bộ theo dõi thay đổi (Change Tracker) của EF Core và tuân thủ các ràng buộc khóa ngoại nếu được cấu hình đúng.
*   **Nhược điểm:** Không reset lại giá trị của cột ID (Auto Increment). Nếu bản ghi cuối cùng có ID là 10, bản ghi mới sau khi Seed sẽ có ID là 11.

### B. Sử dụng `TRUNCATE` (MySQL Specific)
*   **Cơ chế:** Thực thi lệnh SQL `TRUNCATE TABLE`.
*   **Ưu điểm:** Cực nhanh và tự động reset cột ID về lại 1.
*   **Nhược điểm:** MySQL không cho phép `TRUNCATE` các bảng đang có khóa ngoại tham chiếu đến. Bạn phải tắt kiểm tra khóa ngoại (`SET FOREIGN_KEY_CHECKS = 0`) trước khi thực hiện.

---

## 4. Những lưu ý chuyên môn về an toàn dữ liệu

1.  **Thứ tự xóa (Delete Order):** Nếu hệ thống của bạn có quan hệ Cha-Con (ví dụ: `Category` và `Product`), bạn phải xóa dữ liệu ở bảng `Product` trước khi xóa ở bảng `Category` để tránh lỗi ràng buộc toàn vẹn dữ liệu.
2.  **Sử dụng Transaction:** Đối với các hệ thống lớn, bạn nên bao bọc quá trình `ClearData` và `SeedData` trong một `Database Transaction`. Nếu quá trình Seed gặp lỗi, hệ thống sẽ Rollback (khôi phục) lại dữ liệu cũ thay vì để Database ở trạng thái trống rỗng.
3.  **Log thông báo:** Nên thêm các dòng `Console.WriteLine` hoặc `ILogger` vào quá trình Initializer để lập trình viên theo dõi được trạng thái Seeding tại cửa sổ Terminal của Rider.

```csharp
Console.WriteLine("--> Reseting data...");
ClearData(context);
Console.WriteLine("--> Seeding new data...");
SeedData(context);
```

Việc kết hợp Reset và Seeding như trên giúp bạn có một quy trình phát triển cực kỳ linh hoạt, mỗi lần khởi động ứng dụng là một lần bạn có dữ liệu mẫu "sạch" để làm việc.