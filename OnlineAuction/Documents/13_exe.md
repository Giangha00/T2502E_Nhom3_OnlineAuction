# XÂY DỰNG HỆ THỐNG QUẢN LÝ KHÓA HỌC (COURSE MANAGEMENT SYSTEM) _ KHÔNG AI PROMPT

## 1. Mục tiêu dự án
Thiết lập một ứng dụng quản trị khóa học dựa trên nền tảng **ASP.NET Core 8.0 MVC** và hệ quản trị cơ sở dữ liệu **MySQL**. Dự án yêu cầu giải quyết các bài toán về quản lý quan hệ dữ liệu (1-N), tích hợp dịch vụ lưu trữ đám mây bên thứ ba và xử lý nội dung văn bản (Rich Text).

## 2. Đặc tả cơ sở dữ liệu
Hệ thống yêu cầu thiết lập hai thực thể chính với mối quan hệ **Một - Nhiều** (Một danh mục chứa nhiều khóa học):

### 2.1. Thực thể `Category` (Danh mục)
*   **Id (int):** Khóa chính, tự tăng.
*   **Name (string):** Tên danh mục (Yêu cầu duy nhất, ví dụ: Công nghệ thông tin, Kinh tế, Ngoại ngữ).
*   **Description (string):** Mô tả tóm tắt về danh mục.

### 2.2. Thực thể `Course` (Khóa học)
*   **Id (int):** Khóa chính, tự tăng.
*   **Title (string):** Tiêu đề khóa học (Bắt buộc, tối thiểu 10 ký tự).
*   **Description (string):** Mô tả ngắn gọn (Văn bản thuần).
*   **Content (string):** Nội dung chi tiết (Lưu trữ dưới dạng mã HTML).
*   **ImageUrl (string):** Đường dẫn tập tin ảnh lưu trữ trên Cloudinary.
*   **Price (decimal):** Học phí của khóa học.
*   **StartDate (DateTime)**: Ngày bắt đầu khóa học.
*   **EndDate (DateTime)**: Ngày kết thúc khóa học.
*   **CategoryId (int):** Khóa ngoại tham chiếu đến bảng `Category`.
*   **Status (int):** Trạng thái khoá học, 1 là active, 0 là đã xoá.

---

## 3. Yêu cầu chức năng chi tiết

### 3.1. Khởi tạo và Quản trị dữ liệu mẫu (Data Seeding)
*   Triển khai lớp `DbInitializer` thực hiện quy trình:
    1.  Xóa dữ liệu cũ (Reset) tại môi trường phát triển (Development).
    2.  Khởi tạo ít nhất 05 danh mục mặc định.
    3.  Khởi tạo dữ liệu mẫu cho 20 khóa học.

### 3.2. Quản lý danh sách khóa học (Index)
*   Truy xuất danh sách khóa học bao gồm thông tin thực thể liên kết (`Category`).
*   Hiển thị hình ảnh đại diện trực tiếp từ CDN của Cloudinary.
*   Triển khai bộ lọc tìm kiếm theo tiêu đề và phân loại theo danh mục.

### 3.3. Luồng nghiệp vụ Thêm mới và Cập nhật (Create/Update)
*   **Dữ liệu danh mục:** Sử dụng Dropdown list (Select menu) để lựa chọn danh mục.
*   **Daterange Picker:** Được sử dụng để thêm ngày bắt đầu và kết thúc khoá học.
*   **Xử lý hình ảnh:**
    *   Upload ảnh lên Cloudinary.
    *   Lưu trữ URL tuyệt đối được trả về vào trường `ImageUrl` trong cơ sở dữ liệu.
*   **Soạn thảo nội dung:**
    *   Tích hợp **CKEditor 5** vào trường `Content`.
    *   Đảm bảo dữ liệu HTML được đồng bộ chính xác từ bộ soạn thảo vào Model khi gửi yêu cầu (Postback).
*   **Ràng buộc dữ liệu (Validation):**
    *   Kiểm tra tính hợp lệ của dữ liệu ở cả hai phía Client-side và Server-side.
    *   Sử dụng `ModelState` để xử lý và hiển thị thông báo lỗi chi tiết trên giao diện.

### 3.4. Hiển thị nội dung chi tiết (Details)
*   Trình bày thông tin đầy đủ của khóa học.
*   Xử lý hiển thị trường nội dung (`Content`) thông qua phương thức `@Html.Raw()` để đảm bảo trình duyệt render đúng định dạng HTML từ CKEditor.

### 3.5. Xoá thông tin khoá học
*   Có confirm trước khi xoá.
*   Sử dụng js để call action trong controller.
*   Chuyển đổi trạng thái khoá học về 0.

---

## 4. Yêu cầu kỹ thuật và Kiến trúc

*   **Môi trường phát triển:** JetBrains Rider.
*   **Công nghệ lõi:** .NET 8.0 MVC, Entity Framework Core (Pomelo Provider).
*   **Dịch vụ lưu trữ ảnh:** Cloudinary API.
*   **Thư viện UI:** Bootstrap 5 hoặc Tailwinds.
*   **Cấu trúc mã nguồn:**
    *   Thực hiện toàn bộ mã nguồn thủ công (Không sử dụng tính năng Scaffolding).
    *   Sử dụng **ViewModel** để tách biệt dữ liệu giao diện và thực thể cơ sở dữ liệu (Database Entity).
    *   Áp dụng **Dependency Injection** để quản lý DbContext và các Services.

---

## 5. Tiêu chí đánh giá

1.  **Tính ổn định:** Hệ thống hoạt động không lỗi khi thực hiện các thao tác CRUD.
2.  **Tính bảo mật:**
    *   Phòng chống tấn công XSS khi hiển thị nội dung Rich Text.
    *   Sử dụng Anti-forgery tokens cho tất cả các Form.
3.  **Trải nghiệm người dùng:** Giao diện trực quan, xử lý lỗi nhập liệu thân thiện, tốc độ phản hồi của việc upload ảnh tối ưu.
4.  **Chất lượng mã nguồn:** Tuân thủ quy tắc đặt tên, tách biệt rõ ràng các tầng xử lý (Separation of Concerns).

## 6. Hướng dẫn setup model quan hệ.

Để thiết lập quan hệ **Một - Nhiều (One-to-Many)** giữa `Category` và `Course` trong Entity Framework Core, lập trình viên cần thực hiện cấu hình tại các lớp Model (Entities) thông qua các thuộc tính điều hướng (Navigation Properties) và thuộc tính khóa ngoại (Foreign Key).

---

### 1. Định nghĩa thực thể `Category` (Bên "Một")
Trong quan hệ này, một danh mục có thể chứa nhiều khóa học. Ta cần sử dụng một tập hợp (thường là `ICollection`) để đại diện cho danh sách các khóa học liên kết.

```csharp
using System.Collections.ObjectModel;

namespace YourProject.Models;

public class Category
{
    public int Id { get; set; }

    // Thuộc tính điều hướng: Một danh mục có nhiều khóa học
    public virtual ICollection<Course> Courses { get; set; } = new Collection<Course>();
}
```

### 2. Định nghĩa thực thể `Course` (Bên "Nhiều")
Mỗi khóa học sẽ thuộc về duy nhất một danh mục. Ta cần khai báo cả **khóa ngoại (Foreign Key)** và **thuộc tính điều hướng đơn (Reference Navigation Property)**.

```csharp
using System.ComponentModel.DataAnnotations.Schema;

namespace YourProject.Models;

public class Course
{
    public int Id { get; set; }

    // Thuộc tính khóa ngoại (Foreign Key)
    public int CategoryId { get; set; }

    // Thuộc tính điều hướng: Một khóa học thuộc về một danh mục
    [ForeignKey("CategoryId")]
    public virtual Category? Category { get; set; }
}
```

---

### 3. Cấu hình chi tiết qua Fluent API (Tùy chọn nhưng khuyến khích)
Mặc dù EF Core có khả năng tự nhận diện quan hệ thông qua các quy tắc đặt tên (Conventions), việc sử dụng **Fluent API** trong lớp `AppDbContext` giúp tường minh hóa cấu trúc và kiểm soát các ràng buộc như xóa dữ liệu liên lụy (Cascade Delete).

Mở tập tin `Data/AppDbContext.cs` và cập nhật phương thức `OnModelCreating`:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Cấu hình quan hệ Một - Nhiều 
    modelBuilder.Entity<Course>()
        .HasOne(c => c.Category)           // Khóa học có một Danh mục
        .WithMany(cat => cat.Courses)      // Danh mục có nhiều Khóa học
        .HasForeignKey(c => c.CategoryId)  // Chỉ định khóa ngoại là CategoryId
        .OnDelete(DeleteBehavior.Restrict); // Ngăn xóa Danh mục nếu đang có Khóa học tham chiếu đến
}
```

---

### 4. Giải thích các thành phần kỹ thuật

*   **`virtual` Keyword:** Việc đánh dấu thuộc tính điều hướng là `virtual` cho phép EF Core sử dụng cơ chế **Lazy Loading** (chỉ tải dữ liệu liên kết khi thực sự cần truy cập đến).
*   **`ICollection<T>`:** Khởi tạo mặc định `new Collection<T>()` giúp tránh lỗi `NullReferenceException` khi thực hiện thêm mới dữ liệu vào danh sách điều hướng.
*   **`DeleteBehavior.Restrict`:** Trong các hệ thống quản lý, thông thường ta không cho phép xóa một `Category` nếu danh mục đó vẫn đang chứa `Course`. Sử dụng `Restrict` hoặc `NoAction` thay vì `Cascade` giúp bảo vệ toàn vẹn dữ liệu.

### 5. Cách truy vấn dữ liệu liên kết (Eager Loading)
Khi lấy danh sách khóa học ở Controller, để hiển thị được tên danh mục ở View, bạn phải sử dụng phương thức `.Include()`:

```csharp
var courses = await _context.Courses
                            .Include(c => c.Category) // Yêu cầu lấy kèm dữ liệu Category
                            .ToListAsync();
```

### 6. Hiển thị danh sách Category trong Form tạo mới Course.

#### 1. Tại Controller (Action Create)
Truy vấn danh sách danh mục và chuyển sang View thông qua `ViewBag` dưới dạng một `SelectList`.

```csharp
public async Task<IActionResult> Create()
{
    // Lấy danh sách ID và Name để đổ vào Dropdown
    var categories = await _context.Categories.ToListAsync();
    ViewBag.CategoryId = new SelectList(categories, "Id", "Name");
    
    return View();
}
```

#### 2. Tại View (Create.cshtml)
Sử dụng Tag Helper `asp-items` để tự động render các thẻ `<option>` cho HTML `<select>`.

```html
<div class="form-group">
    <label>Danh mục khóa học</label>
    <select asp-for="CategoryId" class="form-control" asp-items="ViewBag.CategoryId">
        <option value="">-- Chọn danh mục --</option>
    </select>
    <span asp-validation-for="CategoryId" class="text-danger"></span>
</div>
```

**Lưu ý:**
*   `asp-for="CategoryId"`: Liên kết giá trị được chọn với thuộc tính `CategoryId` của Model.
*   Nếu dùng **ViewModel**, nên đưa `IEnumerable<SelectListItem>` vào trực tiếp trong ViewModel thay vì dùng `ViewBag` để code chuyên nghiệp và an toàn hơn (Strongly Typed).
    