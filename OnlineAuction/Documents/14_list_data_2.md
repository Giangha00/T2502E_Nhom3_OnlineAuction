Tích hợp bộ lọc tìm kiếm, sắp xếp và công cụ chọn khoảng thời gian **Daterange Picker**.

---

### Bước 1: Tạo ViewModel cho tìm kiếm
Để quản lý các tham số lọc một cách chuyên nghiệp, ta tạo một `UserFilterViewModel`.

```csharp
namespace YourProject.ViewModels;

public class UserFilterViewModel
{
    public string? Keyword { get; set; }
    public string? DateRange { get; set; } // Dạng "MM/DD/YYYY - MM/DD/YYYY"
    public string? SortOrder { get; set; }
    public IEnumerable<User> Users { get; set; } = new List<User>();
}
```

---

### Bước 2: Cập nhật Controller với Logic lọc và sắp xếp
Tại Action `Index`, ta tiếp nhận các tham số từ Form và xây dựng câu lệnh truy vấn động.

```csharp
public async Task<IActionResult> Index(UserFilterViewModel filter)
{
    var query = _context.Users.AsQueryable();

    // 1. Lọc theo từ khóa (Tên hoặc Email)
    if (!string.IsNullOrEmpty(filter.Keyword))
    {
        query = query.Where(u => u.FullName.Contains(filter.Keyword) || u.Email.Contains(filter.Keyword));
    }

    // 2. Lọc theo thời gian (Daterange Picker)
    if (!string.IsNullOrEmpty(filter.DateRange))
    {
        var dates = filter.DateRange.Split(" - ");
        if (dates.Length == 2)
        {
            var startDate = DateTime.ParseExact(dates[0], "MM/DD/YYYY", null);
            var endDate = DateTime.ParseExact(dates[1], "MM/DD/YYYY", null).AddDays(1);
            query = query.Where(u => u.CreatedAt >= startDate && u.CreatedAt < endDate);
        }
    }

    // 3. Sắp xếp
    query = filter.SortOrder switch
    {
        "name_desc" => query.OrderByDescending(u => u.FullName),
        "date_asc" => query.OrderBy(u => u.CreatedAt),
        "date_desc" => query.OrderByDescending(u => u.CreatedAt),
        _ => query.OrderBy(u => u.FullName), // Mặc định
    };

    filter.Users = await query.ToListAsync();
    return View(filter);
}
```

---

### Bước 3: Cài đặt Thư viện Daterange Picker
Thư viện này yêu cầu **jQuery** và **Moment.js**. Thêm các liên kết sau vào trang hoặc file `_Layout.cshtml`.

```html
<!-- CSS -->
<link rel="stylesheet" type="text/css" href="https://cdn.jsdelivr.net/npm/daterangepicker/daterangepicker.css" />

<!-- JS (Đặt sau jQuery) -->
<script type="text/javascript" src="https://cdn.jsdelivr.net/momentjs/latest/moment.min.js"></script>
<script type="text/javascript" src="https://cdn.jsdelivr.net/npm/daterangepicker/daterangepicker.min.js"></script>
```

---

### Bước 4: Cập nhật View (Index.cshtml)
Xây dựng Form tìm kiếm sử dụng Bootstrap 5 và bảng hiển thị dữ liệu.

```razor
@model YourProject.ViewModels.UserFilterViewModel

<div class="container mt-4">
    <h2 class="mb-4">Quản lý người dùng</h2>

    <!-- Form lọc -->
    <form asp-action="Index" method="get" class="row g-3 mb-4 p-3 bg-light border rounded">
        <div class="col-md-3">
            <input type="text" name="Keyword" value="@Model.Keyword" class="form-control" placeholder="Tìm theo tên, email...">
        </div>
        <div class="col-md-3">
            <input type="text" name="DateRange" value="@Model.DateRange" id="daterange" class="form-control" placeholder="Chọn khoảng thời gian">
        </div>
        <div class="col-md-3">
            <select name="SortOrder" class="form-select">
                <option value="name_asc" selected="@(Model.SortOrder == "name_asc")">Tên (A-Z)</option>
                <option value="name_desc" selected="@(Model.SortOrder == "name_desc")">Tên (Z-A)</option>
                <option value="date_desc" selected="@(Model.SortOrder == "date_desc")">Mới nhất</option>
                <option value="date_asc" selected="@(Model.SortOrder == "date_asc")">Cũ nhất</option>
            </select>
        </div>
        <div class="col-md-3 d-flex gap-2">
            <button type="submit" class="btn btn-primary w-100">Tìm kiếm</button>
            <a asp-action="Index" class="btn btn-secondary w-100">Reset</a>
        </div>
    </form>

    <!-- Bảng dữ liệu -->
    <div class="table-responsive">
        <table class="table table-bordered table-hover">
            <thead class="table-dark">
                <tr>
                    <th>Họ tên</th>
                    <th>Email</th>
                    <th>Ngày tạo</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var user in Model.Users)
                {
                    <tr>
                        <td>@user.FullName</td>
                        <td>@user.Email</td>
                        <td>@user.CreatedAt.ToString("dd/MM/yyyy HH:mm")</td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
</div>

@section Scripts {
    <script>
        $(function() {
            $('#daterange').daterangepicker({
                autoUpdateInput: false,
                locale: {
                    cancelLabel: 'Clear',
                    format: 'MM/DD/YYYY'
                }
            });

            $('#daterange').on('apply.daterangepicker', function(ev, picker) {
                $(this).val(picker.startDate.format('MM/DD/YYYY') + ' - ' + picker.endDate.format('MM/DD/YYYY'));
            });

            $('#daterange').on('cancel.daterangepicker', function(ev, picker) {
                $(this).val('');
            });
        });
    </script>
}
```

---

### Giải thích các thành phần kỹ thuật chuyên sâu:

1.  **Method="GET":** Sử dụng phương thức GET cho bộ lọc giúp người dùng có thể copy URL đã lọc hoặc quay lại trang trước (Back) mà không bị mất dữ liệu tìm kiếm.
2.  **IQueryable:** Việc xây dựng truy vấn trên `IQueryable` giúp tối ưu hiệu suất vì câu lệnh lọc chỉ được thực thi và gửi tới MySQL khi gọi `ToListAsync()`.
3.  **Date Parsing:** Lưu ý định dạng ngày tháng trong `DateTime.ParseExact` phải khớp hoàn toàn với định dạng trong cấu hình `daterangepicker` của JavaScript.
4.  **AutoUpdateInput: false:** Cấu hình này giúp ô nhập liệu ngày tháng để trống khi mới tải trang, chỉ hiện giá trị khi người dùng thực sự chọn một khoảng thời gian.
5.  **SQL Range:** Khi lọc theo ngày, chúng ta thêm `.AddDays(1)` vào ngày kết thúc để đảm bảo lấy được dữ liệu của cả ngày cuối cùng (vì `DateTime` mặc định là 00:00:00).