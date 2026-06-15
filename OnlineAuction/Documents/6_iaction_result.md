Trong ASP.NET Core, `IActionResult` là một interface định nghĩa hợp đồng cho kết quả của một Action method. Nó cho phép một phương thức trả về nhiều loại phản hồi HTTP khác nhau (HTML, JSON, File, hoặc các mã trạng thái như 404, 401).

---

### 1. ViewResult
`ViewResult` là kiểu trả về quan trọng nhất trong kiến trúc MVC. Nó chỉ định hệ thống Razor View Engine tìm kiếm, biên dịch tập tin `.cshtml` và trả về mã HTML hoàn chỉnh cho trình duyệt.

*   **Mục đích:** Trả về một giao diện người dùng (giao diện HTML).
*   **Dữ liệu trả về:**
    *   **HTTP Status Code:** 200 OK.
    *   **Content-Type:** `text/html`.
    *   **Body:** Tài liệu HTML đã được render từ tập tin Razor.

**Code mẫu:**
```csharp
public IActionResult Details(int id)
{
    var product = _context.Products.Find(id);
    if (product == null)
    {
        return NotFound(); // Trả về NotFoundResult (404)
    }
    // Trả về ViewResult kèm theo đối tượng Model
    return View(product); 
}
```

---

### 2. RedirectToActionResult
Đây là kiểu trả về thực hiện chuyển hướng trình duyệt tới một Action hoặc Controller khác. Trong mô hình CRUD, nó cực kỳ quan trọng để thực hiện mô hình **PRG (Post-Redirect-Get)** nhằm tránh việc người dùng nhấn F5 để gửi lại dữ liệu (Resubmit form).

*   **Mục đích:** Điều hướng người dùng sang một trang khác sau khi xử lý logic thành công (như sau khi Thêm mới hoặc Xóa).
*   **Dữ liệu trả về:**
    *   **HTTP Status Code:** 302 Found.
    *   **Header:** `Location` chứa URL của Action đích.
    *   **Body:** Trống.

**Code mẫu:**
```csharp
[HttpPost]
public IActionResult Create(Product product)
{
    if (ModelState.IsValid)
    {
        _context.Products.Add(product);
        _context.SaveChanges();
        
        // Chuyển hướng về trang danh sách sản phẩm sau khi lưu thành công
        return RedirectToAction("Index", "Products");
    }
    return View(product);
}
```

---

### 3. JsonResult
Mặc dù bạn đang làm việc với View, `JsonResult` vẫn thường xuyên được sử dụng khi bạn cần xử lý các tác vụ bất đồng bộ (AJAX) trên giao diện (ví dụ: cập nhật giỏ hàng mà không tải lại trang).

*   **Mục đích:** Trả về dữ liệu dưới dạng chuỗi JSON thay vì HTML. Thường dùng cho các yêu cầu gọi từ JavaScript (JQuery, Axios).
*   **Dữ liệu trả về:**
    *   **HTTP Status Code:** 200 OK.
    *   **Content-Type:** `application/json`.
    *   **Body:** Chuỗi dữ liệu JSON của đối tượng được truyền vào.

**Code mẫu:**
```csharp
[HttpGet]
public IActionResult GetStock(int id)
{
    var stockCount = _context.Products
                             .Where(p => p.Id == id)
                             .Select(p => p.Quantity)
                             .FirstOrDefault();

    // Trả về một đối tượng ẩn danh dưới dạng JSON
    return Json(new { productId = id, availableStock = stockCount });
}
```

---

### Bảng tóm tắt các Implementations phổ biến khác

Ngoài 3 loại trên, lập trình viên .NET thường xuyên sử dụng các loại sau để xử lý các tình huống nghiệp vụ cụ thể:

| Implementation | Phương thức hỗ trợ | Mục đích | Mã HTTP |
| :--- | :--- | :--- | :--- |
| **NotFoundResult** | `NotFound()` | Thông báo không tìm thấy tài nguyên | 404 |
| **BadRequestResult** | `BadRequest()` | Thông báo yêu cầu của Client không hợp lệ | 400 |
| **FileResult** | `File()` | Trả về tập tin (PDF, Image, Excel...) để tải về | 200 |
| **PartialViewResult** | `PartialView()` | Trả về một phần của HTML (không có Layout) | 200 |
| **ContentResult** | `Content()` | Trả về một chuỗi văn bản thuần túy | 200 |

**Nguyên tắc sử dụng chuyên nghiệp:**
Luôn sử dụng kiểu trả về là `IActionResult` (Interface) cho Action method thay vì các kiểu cụ thể (như `ViewResult`). Điều này mang lại sự linh hoạt, cho phép bạn trả về `View()` khi thành công và `NotFound()` hoặc `BadRequest()` khi gặp lỗi trong cùng một phương thức.