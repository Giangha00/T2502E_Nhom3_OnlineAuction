Trong .NET 8, cơ chế **Data Validation** (Kiểm tra tính hợp lệ của dữ liệu) chủ yếu dựa trên các **Data Annotations**. Đây là những thuộc tính (Attributes) được áp dụng trực tiếp lên các thuộc tính của Model nhằm thiết lập các quy tắc nghiệp vụ ngay tại tầng dữ liệu.

---

# DATA VALIDATION TRONG .NET 8: TẦNG MODEL

## 1. Các bộ Validators phổ biến (Built-in Validators)

Tất cả các thuộc tính dưới đây nằm trong namespace: `System.ComponentModel.DataAnnotations`.

| Validator | Mục đích | Trường hợp sử dụng | Code mẫu |
| :--- | :--- | :--- | :--- |
| **`[Required]`** | Bắt buộc nhập | Tên sản phẩm, Email, Mật khẩu | `[Required(ErrorMessage = "Bắt buộc nhập tên")]` |
| **`[StringLength]`** | Giới hạn độ dài chuỗi | Giới hạn ký tự cho mô tả hoặc tên | `[StringLength(100, MinimumLength = 10)]` |
| **`[MinLength / MaxLength]`** | Độ dài mảng/chuỗi | Kiểm tra số lượng phần tử hoặc ký tự | `[MinLength(5)]` |
| **`[Range]`** | Giới hạn khoảng giá trị | Giá sản phẩm, Số lượng kho, Tuổi | `[Range(0, 1000, ErrorMessage = "Từ 0-1000")]` |
| **`[Compare]`** | So sánh hai thuộc tính | Xác nhận lại mật khẩu | `[Compare("Password", ErrorMessage = "Không khớp")]` |
| **`[EmailAddress]`** | Định dạng Email | Kiểm tra cấu trúc email hợp lệ | `[EmailAddress(ErrorMessage = "Email sai định dạng")]` |
| **`[Phone]`** | Định dạng số điện thoại | Kiểm tra số điện thoại (quốc tế) | `[Phone]` |
| **`[Url]`** | Định dạng đường dẫn | Kiểm tra địa chỉ website | `[Url]` |
| **`[RegularExpression]`** | Kiểm tra theo mẫu (Regex) | Độ phức tạp mật khẩu, Mã định danh riêng | `[RegularExpression(@"^[A-Z]+[a-zA-Z]*$")]` |

**Ví dụ Model hoàn chỉnh:**
```csharp
public class UserRegistrationModel
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [StringLength(20, MinimumLength = 6)]
    public string Password { get; set; } = string.Empty;

    [Compare("Password")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
```

---

## 2. Remote Validation: Kiểm tra dữ liệu thông qua API

Đây là trường hợp bạn cần validate dữ liệu mà logic kiểm tra nằm ở Database hoặc một API bên ngoài (ví dụ: Kiểm tra xem Username đã tồn tại chưa).

### Cơ chế hoạt động:
Khi người dùng nhập liệu, trình duyệt sẽ tự động gửi một yêu cầu AJAX ngầm đến một Action trong Controller để kiểm tra. Action này trả về `true` (hợp lệ) hoặc một `chuỗi văn bản` (thông báo lỗi).

### Bước 1: Cấu hình tại Model
Sử dụng thuộc tính `[Remote]`. Bạn cần cài đặt thư viện `Microsoft.AspNetCore.Mvc.ViewFeatures`.

```csharp
using Microsoft.AspNetCore.Mvc; // Cần thư viện này cho Remote Attribute

public class UserModel
{
    [Required]
    [Remote(action: "IsUsernameAvailable", controller: "Users", ErrorMessage = "Tên đăng nhập này đã được sử dụng.")]
    public string Username { get; set; } = string.Empty;
}
```

### Bước 2: Xử lý tại Controller
Tạo Action tương ứng để thực hiện việc gọi API hoặc truy vấn Database.

```csharp
public class UsersController : Controller
{
    private readonly AppDbContext _context;
    public UsersController(AppDbContext context) => _context = context;

    [HttpGet] // Remote Validation luôn dùng GET
    public async Task<IActionResult> IsUsernameAvailable(string username)
    {
        // Giả sử gọi API hoặc kiểm tra DB
        bool isExisted = await _context.Users.AnyAsync(u => u.Username == username);

        if (isExisted)
        {
            return Json("Tên đăng nhập đã tồn tại."); // Trả về thông báo lỗi
        }

        return Json(true); // Trả về true nếu hợp lệ
    }
}
```

### Bước 3: Cấu hình tại View
Để tính năng này hoạt động, bạn phải chèn các thư viện Script validation vào View:
```razor
@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```

---

## 3. Custom Validation Logic (IValidatableObject)

Trong trường hợp cần validate phức tạp liên quan đến nhiều thuộc tính cùng lúc (mà không cần gọi API ngầm), bạn có thể implement interface `IValidatableObject`.

```csharp
public class OrderModel : IValidatableObject
{
    public DateTime OrderDate { get; set; }
    public DateTime DeliveryDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (DeliveryDate < OrderDate)
        {
            yield return new ValidationResult(
                "Ngày giao hàng không được trước ngày đặt hàng.",
                new[] { nameof(DeliveryDate) }
            );
        }
    }
}
```

---

## 4. Tổng kết quy tắc sử dụng

1.  **Validator tĩnh (`[Required]`, `[Range]`...)**: Dùng cho các quy tắc định dạng và khoảng giá trị cố định. Ưu điểm là thực thi nhanh cả ở Client và Server.
2.  **Remote Validator (`[Remote]`)**: Dùng khi cần kiểm tra tính duy nhất hoặc kiểm tra qua Database/API bên thứ ba ngay khi người dùng đang nhập liệu.
3.  **Server-side Manual Validation**: Luôn luôn kiểm tra lại `if (!ModelState.IsValid)` tại Controller vì người dùng có thể tắt JavaScript ở trình duyệt để vượt qua lớp bảo vệ phía Client.

Việc kết hợp chặt chẽ các tầng kiểm tra này giúp ứng dụng đảm bảo tính toàn vẹn của dữ liệu và mang lại trải nghiệm mượt mà cho người dùng.