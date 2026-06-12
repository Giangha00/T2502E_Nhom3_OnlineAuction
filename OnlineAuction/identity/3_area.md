# TỔ CHỨC VÀ THIẾT LẬP AREA TRONG .NET 8 MVC

## 1. Khái niệm về Area
**Area** thực chất là một "ứng dụng nhỏ" bên trong ứng dụng chính. Mỗi Area có cấu trúc thư mục riêng gồm **Controllers**, **Models**, và **Views**.

*   **Lợi ích:** Tách biệt logic quản trị (Admin) khỏi logic người dùng (Client), giúp nhiều nhóm lập trình viên có thể làm việc trên các mô-đun khác nhau mà không bị xung đột.

---

## 2. Các bước thiết lập trên JetBrains Rider

### Bước 1: Tạo cấu trúc thư mục Area
Rider hỗ trợ tính năng Scaffolding (sinh mã tự động), nhưng để kiểm soát tốt nhất, bạn nên thực hiện thủ công hoặc dùng tính năng **New Area** của Rider:

1.  Chuột phải vào Project trong cửa sổ **Solution**.
2.  Chọn **Add** -> **New Folder** đặt tên là `Areas`.
3.  Chuột phải vào thư mục `Areas` vừa tạo -> **Add** -> **Area...** (nếu có) hoặc tự tạo thư mục con theo cấu trúc:
    *   `Areas/Admin/Controllers/`
    *   `Areas/Admin/Models/`
    *   `Areas/Admin/Views/`

### Bước 2: Khai báo thuộc tính [Area] tại Controller
Để hệ thống định tuyến (Routing) nhận diện được Controller thuộc Area nào, bạn **bắt buộc** phải sử dụng thuộc tính `[Area]`.

*Tập tin: `Areas/Admin/Controllers/DashboardController.cs`*
```csharp
using Microsoft.AspNetCore.Mvc;

namespace YourProject.Areas.Admin.Controllers;

[Area("Admin")] // Phải trùng khớp với tên thư mục Area
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
```

### Bước 3: Cấu hình Định tuyến (Routing) trong Program.cs
Bạn cần khai báo cho ASP.NET Core biết cách xử lý URL có chứa Area (thông thường là `/AreaName/ControllerName/ActionName`).

*Tập tin: `Program.cs`*
```csharp
app.MapControllerRoute(
    name: "MyArea",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```
**Lưu ý:** Route có chứa `{area:exists}` phải nằm **TRƯỚC** Route mặc định (`default`).

---

## 3. Quản lý View trong Area

Mỗi Area cần có cơ chế quản lý giao diện riêng để hoạt động độc lập.

1.  **Cấu trúc Views:** Trong `Areas/Admin/Views/`, bạn nên tạo thêm thư mục tương ứng với tên Controller (ví dụ: `Dashboard/Index.cshtml`).
2.  **ViewImports và ViewStart:** Để sử dụng Tag Helpers và Layout chung, hãy copy tập tin `_ViewImports.cshtml` và `_ViewStart.cshtml` từ thư mục `Views` gốc vào `Areas/Admin/Views/`.
    *   Nếu bạn muốn Admin sử dụng một giao diện khác hoàn toàn, hãy tạo `_Layout.cshtml` mới bên trong `Areas/Admin/Views/Shared/`.

---

## 4. Cách tạo liên kết (Link) giữa các Area

Khi sử dụng Tag Helpers để tạo liên kết, bạn cần chỉ định rõ tham số `asp-area`.

*   **Từ trang User vào trang Admin:**
    ```html
    <a asp-area="Admin" asp-controller="Dashboard" asp-action="Index">Vào trang quản trị</a>
    ```
*   **Từ trang Admin quay về trang chủ (ngoài Area):**
    ```html
    <a asp-area="" asp-controller="Home" asp-action="Index">Quay về trang chủ</a>
    ```
    *(Để `asp-area=""` trống để quay về các Controller không nằm trong Area).*

---

## 5. Mẹo chuyên nghiệp khi dùng Rider với Area

1.  **Phân quyền (Authorization):** Để đảm bảo bảo mật cho toàn bộ Area, thay vì đặt `[Authorize(Roles = "Admin")]` trên từng Controller, bạn nên tạo một **BaseAdminController**:
    ```csharp
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public abstract class BaseAdminController : Controller { }
    ```
    Sau đó, tất cả các Controller trong thư mục `Areas/Admin/` sẽ kế thừa từ lớp này.
2.  **Navigation (Shift + Shift):** Rider cho phép bạn tìm kiếm nhanh các tập tin trong Area bằng cách nhấn phím Shift 2 lần và gõ tên Controller.
3.  **Namespace:** Đảm bảo Namespace của Controller trong Area phản ánh đúng cấu trúc thư mục (ví dụ: `ProjectName.Areas.Admin.Controllers`) để tránh xung đột với các Controller trùng tên ở vùng ngoài.

Việc sử dụng Area kết hợp với việc kế thừa **BaseAdminController** là cách thức tối ưu nhất để quản trị dự án .NET 8 MVC một cách sạch sẽ và bảo mật.