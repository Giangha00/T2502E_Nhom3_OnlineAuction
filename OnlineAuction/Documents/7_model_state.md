Trong kiến trúc ASP.NET Core MVC, **ModelState** là một thành phần đóng vai trò cầu nối quan trọng giữa dữ liệu người dùng gửi lên (Client) và việc kiểm tra tính hợp lệ của dữ liệu đó tại máy chủ (Server).

---

# CHI TIẾT VỀ MODELSTATE VÀ CƠ CHẾ HIỂN THỊ LỖI TRONG .NET 8 MVC

## 1. ModelState là gì?
`ModelState` là một đối tượng thuộc lớp `ModelStateDictionary`, được tạo ra trong quá trình **Model Binding**. Nó chứa hai thông tin chính:
1.  **Giá trị của Model:** Lưu trữ các giá trị mà người dùng đã nhập vào Form.
2.  **Trạng thái Validation:** Lưu trữ các thông tin về việc liệu các giá trị đó có vi phạm các quy tắc dữ liệu (Validation Rules) hay không.

### Các thuộc tính quan trọng:
*   **`ModelState.IsValid`**: Trả về `true` nếu không có bất kỳ lỗi nào được ghi nhận. Đây là "chốt chặn" quan trọng nhất trước khi xử lý dữ liệu vào Database.
*   **`ModelState.ErrorCount`**: Tổng số lỗi hiện có.

---

## 2. Cách thức hoạt động của Validation

### Bước 1: Khai báo quy tắc tại Model (Data Annotations)
Sử dụng các thuộc tính (Attributes) để định nghĩa quy tắc ngay tại Model:
```csharp
public class Product
{
    [Required(ErrorMessage = "Tên không được để trống")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên phải từ 3-100 ký tự")]
    public string Name { get; set; }

    [Range(1, 1000, ErrorMessage = "Giá phải nằm trong khoảng 1-1000")]
    public decimal Price { get; set; }
}
```

### Bước 2: Kiểm tra tại Controller
Khi người dùng gửi Form, .NET sẽ tự động đối chiếu dữ liệu với các quy tắc trên:
```csharp
[HttpPost]
public IActionResult Create(Product product)
{
    // Kiểm tra tính hợp lệ của toàn bộ Model
    if (!ModelState.IsValid)
    {
        // Nếu có lỗi, trả về View cùng với Model để hiển thị thông báo lỗi
        return View(product);
    }
    
    // Nếu hợp lệ, tiến hành lưu Database
    _context.Add(product);
    _context.SaveChanges();
    return RedirectToAction("Index");
}
```

---

## 3. Thêm lỗi tùy chỉnh (Manual Validation)
Đôi khi các Data Annotations không đủ (ví dụ: kiểm tra trùng tên trong DB). Bạn có thể tự thêm lỗi vào `ModelState`:
```csharp
if (_context.Products.Any(p => p.Name == product.Name))
{
    // "Name" là tên thuộc tính bị gán lỗi
    ModelState.AddModelError("Name", "Tên sản phẩm này đã tồn tại trong hệ thống.");
}
```

---

## 4. Cách hiển thị lỗi trong View (Razor Tag Helpers)

ASP.NET Core cung cấp 2 cách chính để hiển thị lỗi thông qua các Tag Helpers chuyên dụng:

### 4.1. Hiển thị tổng hợp lỗi (Validation Summary)
Dùng để hiển thị tất cả các lỗi của Form tại một vị trí duy nhất (thường là ở đầu trang).
```html
<!-- ModelOnly: Chỉ hiển thị lỗi chung của Model, không hiển thị lỗi của từng thuộc tính -->
<!-- All: Hiển thị tất cả mọi lỗi -->
<div asp-validation-summary="All" class="text-danger"></div>
```

### 4.2. Hiển thị lỗi theo từng trường (Validation Message)
Dùng để hiển thị thông báo lỗi ngay dưới hoặc cạnh ô nhập liệu bị sai.
```html
<div class="form-group">
    <label asp-for="Name"></label>
    <input asp-for="Name" class="form-control" />
    
    <!-- Hiển thị lỗi cụ thể của trường Name -->
    <span asp-validation-for="Name" class="text-danger"></span>
</div>
```

---

## 5. Client-side Validation (Kiểm tra phía trình duyệt)

Để tăng trải nghiệm người dùng (báo lỗi ngay khi gõ mà không cần tải lại trang), .NET hỗ trợ **Client-side Validation**.

### Cách kích hoạt:
Bạn cần chèn các thư viện script của jQuery Validation vào cuối file View (thường nằm trong thẻ `scripts` của Layout):
```razor
@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```
**Cơ chế:** Khi có các script này, các thuộc tính `[Required]`, `[Range]`... sẽ được chuyển thành các thuộc tính `data-val-*` trong HTML. Thư viện jQuery sẽ đọc các thuộc tính này để chặn việc gửi Form nếu dữ liệu không hợp lệ.

---

## 6. Tổng kết quy trình xử lý lỗi chuyên nghiệp

1.  **Model:** Định nghĩa các ràng buộc bằng **Data Annotations**.
2.  **View:** Sử dụng `asp-validation-for` cho từng ô nhập liệu và chèn `_ValidationScriptsPartial` để báo lỗi nhanh.
3.  **Controller:** Luôn kiểm tra `if (!ModelState.IsValid)`. Nếu sai, trả lại `View(model)` để các Tag Helpers tự động điền lại dữ liệu cũ và hiển thị lỗi.
4.  **Database:** `ModelState` là bước bảo vệ cuối cùng trước khi dữ liệu đi vào tầng lưu trữ.

Việc quản lý tốt `ModelState` giúp ứng dụng của bạn trở nên chặt chẽ, tránh được các dữ liệu "rác" và cung cấp phản hồi rõ ràng cho người dùng cuối.