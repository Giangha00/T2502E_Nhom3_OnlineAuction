# Phân quyền theo các **Path (đường dẫn)** cụ thể

### Cách 1: Sử dụng "Areas" kết hợp với Global Filter (Khuyên dùng)

Trong kiến trúc MVC, các vùng quản trị thường được tách vào một **Area** tên là `Admin`. Đây là cách tốt nhất để tổ chức thư mục và cũng là cách dễ nhất để áp dụng phân quyền hàng loạt.

1.  **Cấu trúc thư mục:** Bạn để các Controller vào thư mục `Areas/Admin/Controllers/`.
2.  **Cấu hình trong `Program.cs`:** Bạn thêm một bộ lọc (Filter) toàn cục nhưng chỉ áp dụng cho vùng `Admin`.

```csharp
builder.Services.AddControllersWithViews(options =>
{
    // Tạo một Policy yêu cầu Role Admin
    var adminPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole("Admin")
        .Build();

    // Áp dụng Filter này cho tất cả các Controller thuộc Area "Admin"
    options.Filters.Add(new AuthorizeFilter(adminPolicy));
})
// Quan trọng: Chỉ áp dụng Filter trên cho đúng Area Admin
.AddMvcOptions(options => {
    // Đoạn này có thể tùy biến thêm nếu muốn lọc theo Namespace hoặc Path
});
```
*Lưu ý: Cách này yêu cầu bạn cài thêm Package `Microsoft.AspNetCore.Mvc.Authorization`.*

---

### Cách 2: Sử dụng "Convention" để lọc theo Namespace hoặc Path (Rất mạnh mẽ)

Nếu bạn không muốn dùng Area mà chỉ muốn dựa vào việc bạn đặt Controller ở đâu (Namespace) hoặc đường dẫn URL, bạn có thể dùng **Application Models**.

1.  **Tạo một lớp Convention:**

```csharp
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Authorization;

public class AdminAreaAuthorizationConvention : IControllerModelConvention
{
    public void Apply(ControllerModel controller)
    {
        // Kiểm tra nếu Controller nằm trong Namespace có chữ ".Admin" 
        // Hoặc kiểm tra thuộc tính Area
        if (controller.Attributes.Any(a => a is AreaAttribute area && area.RouteValue == "Admin") 
            || controller.ControllerNamespace.Contains(".Admin"))
        {
            controller.Filters.Add(new AuthorizeFilter("AdminPolicy"));
        }
    }
}
```

2.  **Đăng ký Policy và Convention trong `Program.cs`:**

```csharp
// 1. Định nghĩa Policy
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Admin"));
});

// 2. Đăng ký Convention
builder.Services.AddControllersWithViews(options =>
{
    options.Conventions.Add(new AdminAreaAuthorizationConvention());
});
```

---

### Cách 3: Sử dụng Authorization Middleware với Endpoint Routing (Cách mới nhất của .NET 8)

Nếu bạn muốn kiểm soát chính xác theo kiểu "Path nào thì quyền đó" ngay tại luồng đi của Request, bạn có thể cấu hình trong phần Endpoint mapping ở cuối file `Program.cs`.

```csharp
app.MapControllerRoute(
    name: "MyArea",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" }); 
    // Dòng này bắt buộc TẤT CẢ các Area phải có quyền Admin mới vào được
```

**Hoặc dùng MapGroup (nếu bạn dùng Minimal API hoặc muốn nhóm cụ thể):**

```csharp
// Mọi Request bắt đầu bằng /admin đều yêu cầu Role Admin
app.MapControllers().RequireAuthorization(new AuthorizeAttribute { Roles = "Admin" });
```

---

### Cách 4: Sử dụng Middleware tùy chỉnh (Nếu muốn kiểm soát "cứng" theo chuỗi URL)

Đây là cách can thiệp sâu nhất, kiểm tra trực tiếp chuỗi URL người dùng gõ vào.

```csharp
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value;
    
    if (path.StartsWith("/admin", StringComparison.OrdinalIgnoreCase))
    {
        if (!context.User.Identity.IsAuthenticated)
        {
            context.Response.Redirect("/Account/Login");
            return;
        }

        if (!context.User.IsInRole("Admin"))
        {
            context.Response.Redirect("/Account/AccessDenied");
            return;
        }
    }
    await next();
});

app.UseAuthentication();
app.UseAuthorization();
```

---

### Lời khuyên chọn lựa:

1.  **Nên dùng Cách 1 (Areas):** Đây là chuẩn mực của dân .NET chuyên nghiệp. Nó tách biệt hoàn toàn code của Admin và code của User (View, Controller, Assets) và phân quyền cực kỳ sạch sẽ.
2.  **Nên dùng Cách 2:** Nếu bạn có cấu trúc thư mục phức tạp và không muốn đổi sang Area.
3.  **Tránh dùng Cách 4:** Trừ khi bạn có những logic kiểm tra đường dẫn cực kỳ dị biệt, vì viết Middleware thủ công dễ gây lỗi logic bảo mật nếu không xử lý hết các trường hợp URL.

**Chiến lược tốt nhất hiện nay:** Chia vùng Admin vào **Area "Admin"**, sau đó dùng **Global Filter** hoặc đặt một `[Authorize(Roles = "Admin")]` lên một **BaseAdminController** và cho tất cả Admin Controller khác kế thừa từ nó.