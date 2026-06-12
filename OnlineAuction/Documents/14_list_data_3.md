Triển khai tính năng phân trang (Pagination) kết hợp với tìm kiếm, lọc và sắp xếp, mã nguồn cần được cấu trúc lại để quản lý số lượng bản ghi trên mỗi trang và tính toán tổng số trang dựa trên dữ liệu đã lọc.

---

### Bước 1: Tạo lớp hỗ trợ phân trang (PaginatedList)

Lớp này sẽ đảm nhận nhiệm vụ tính toán số trang và sử dụng các phương thức `Skip()` và `Take()` của LINQ để tối ưu truy vấn SQL.

*Tập tin: `Helpers/PaginatedList.cs`*

```csharp
using Microsoft.EntityFrameworkCore;

namespace YourProject.Helpers;

public class PaginatedList<T> : List<T>
{
    public int PageIndex { get; private set; }
    public int TotalPages { get; private set; }

    public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        this.AddRange(items);
    }

    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
    {
        var count = await source.CountAsync();
        var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PaginatedList<T>(items, count, pageIndex, pageSize);
    }
}
```

---

### Bước 2: Cập nhật ViewModel

Bổ sung thuộc tính `PageIndex` để lưu trữ trạng thái trang hiện tại.

```csharp
using YourProject.Helpers;

namespace YourProject.ViewModels;

public class UserFilterViewModel
{
    public string? Keyword { get; set; }
    public string? DateRange { get; set; }
    public string? SortOrder { get; set; }
    public int? PageIndex { get; set; } // Trang hiện tại
    
    // Sử dụng lớp PaginatedList thay cho IEnumerable
    public PaginatedList<User> Users { get; set; } 
}
```

---

### Bước 3: Cập nhật Controller

Action `Index` sẽ tiếp nhận tham số `pageIndex` và thực hiện phân trang sau khi đã áp dụng các bộ lọc.

```csharp
public async Task<IActionResult> Index(UserFilterViewModel filter, int? pageIndex)
{
    var query = _context.Users.AsQueryable();

    // 1. Logic Lọc (Giữ nguyên như phần trước)
    if (!string.IsNullOrEmpty(filter.Keyword))
    {
        query = query.Where(u => u.FullName.Contains(filter.Keyword) || u.Email.Contains(filter.Keyword));
    }

    // 2. Logic Thời gian (Sửa lại định dạng chuẩn C#)
    if (!string.IsNullOrEmpty(filter.DateRange))
    {
        var dates = filter.DateRange.Split(" - ");
        if (dates.Length == 2)
        {
            var startDate = DateTime.ParseExact(dates[0], "MM/dd/yyyy", null);
            var endDate = DateTime.ParseExact(dates[1], "MM/dd/yyyy", null).AddDays(1);
            query = query.Where(u => u.CreatedAt >= startDate && u.CreatedAt < endDate);
        }
    }

    // 3. Logic Sắp xếp
    query = filter.SortOrder switch
    {
        "name_desc" => query.OrderByDescending(u => u.FullName),
        "date_asc" => query.OrderBy(u => u.CreatedAt),
        "date_desc" => query.OrderByDescending(u => u.CreatedAt),
        _ => query.OrderBy(u => u.FullName),
    };

    // 4. Thực thi Phân trang
    int pageSize = 10; // Số lượng bản ghi trên mỗi trang
    filter.Users = await PaginatedList<User>.CreateAsync(query.AsNoTracking(), pageIndex ?? 1, pageSize);

    return View(filter);
}
```

---

### Bước 4: Cập nhật View (Hiển thị thanh phân trang Bootstrap)

Điểm quan trọng nhất khi phân trang kèm bộ lọc là các liên kết chuyển trang phải mang theo các tham số tìm kiếm cũ (Query String).

*Tập tin: `Views/User/Index.cshtml`*

```razor
@model YourProject.ViewModels.UserFilterViewModel

<div class="container mt-4">
    <!-- [Phần Form Tìm kiếm giữ nguyên như hướng dẫn trước] -->

    <!-- Bảng dữ liệu -->
    <table class="table table-bordered">
        <!-- [Phần thead và tbody hiển thị Model.Users] -->
    </table>

    <!-- Thanh phân trang -->
    @{
        var prevDisabled = !Model.Users.HasPreviousPage ? "disabled" : "";
        var nextDisabled = !Model.Users.HasNextPage ? "disabled" : "";
    }

    <nav aria-label="Page navigation">
        <ul class="pagination justify-content-center">
            <li class="page-item @prevDisabled">
                <a class="page-link" 
                   asp-action="Index" 
                   asp-route-pageIndex="@(Model.Users.PageIndex - 1)"
                   asp-route-Keyword="@Model.Keyword"
                   asp-route-DateRange="@Model.DateRange"
                   asp-route-SortOrder="@Model.SortOrder">Trước</a>
            </li>

            @for (int i = 1; i <= Model.Users.TotalPages; i++)
            {
                <li class="page-item @(i == Model.Users.PageIndex ? "active" : "")">
                    <a class="page-link" 
                       asp-action="Index" 
                       asp-route-pageIndex="@i"
                       asp-route-Keyword="@Model.Keyword"
                       asp-route-DateRange="@Model.DateRange"
                       asp-route-SortOrder="@Model.SortOrder">@i</a>
                </li>
            }

            <li class="page-item @nextDisabled">
                <a class="page-link" 
                   asp-action="Index" 
                   asp-route-pageIndex="@(Model.Users.PageIndex + 1)"
                   asp-route-Keyword="@Model.Keyword"
                   asp-route-DateRange="@Model.DateRange"
                   asp-route-SortOrder="@Model.SortOrder">Sau</a>
            </li>
        </ul>
    </nav>
</div>
```

---

### Phân tích kỹ thuật chuyên sâu:

1.  **Hiệu suất truy vấn (Server-side Pagination):** Việc sử dụng `Skip()` và `Take()` giúp MySQL chỉ trả về đúng số bản ghi cần hiển thị (ví dụ 10 dòng) thay vì tải toàn bộ hàng nghìn dòng lên RAM. Điều này là bắt buộc đối với các hệ thống lớn.
2.  **Duy trì trạng thái (State Persistence):** Trong mã Razor, các tham số `asp-route-Keyword`, `asp-route-DateRange`... đảm bảo rằng khi người dùng nhấn sang trang 2, các điều kiện tìm kiếm và khoảng thời gian đã chọn không bị mất đi.
3.  **AsNoTracking():** Khi thực hiện truy vấn chỉ để hiển thị (Read-only), sử dụng `AsNoTracking()` giúp tăng tốc độ xử lý do EF Core không cần thiết lập bộ theo dõi thay đổi cho các đối tượng.
4.  **UX Phân trang:**
    *   Các nút "Trước" và "Sau" được kiểm soát bởi thuộc tính `HasPreviousPage` và `HasNextPage`.
    *   Sử dụng class `disabled` của Bootstrap để ngăn chặn các thao tác không hợp lệ khi người dùng ở trang đầu tiên hoặc cuối cùng.
5.  **Daterange Picker:** Lưu ý khi phân trang, giá trị của ô `DateRange` cần được gán lại vào Input thông qua `value="@Model.DateRange"` để JavaScript khởi tạo lại đúng khoảng thời gian người dùng đã chọn.