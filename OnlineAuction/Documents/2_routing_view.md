# CƠ CHẾ ĐỊNH TUYẾN VÀ TƯƠNG TÁC CONTROLLER-VIEW TRONG .NET 8

## 1. Cơ chế định tuyến (Routing System)

Routing là thành phần chịu trách nhiệm phân tích các yêu cầu HTTP (HTTP Requests) từ trình duyệt và ánh xạ (mapping) chúng tới các bộ xử lý tương ứng trong mã nguồn.

### 1.1. Định tuyến mặc định (Conventional Routing)
Trong dự án MVC, định tuyến thường được cấu hình tập trung tại tập tin `Program.cs`. Cấu hình mặc định phổ biến nhất như sau:

```csharp
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

**Phân tích cấu trúc Pattern:**
*   **`{controller=Home}`**: Xác định Controller sẽ xử lý. Nếu URL không chỉ định, giá trị mặc định là `HomeController`.
*   **`{action=Index}`**: Xác định phương thức (Action) bên trong Controller. Nếu không chỉ định, giá trị mặc định là `Index`.
*   **`{id?}`**: Tham số tùy chọn (Optional). Dấu `?` cho biết tham số này có thể có hoặc không.

**Ví dụ ánh xạ:**
| URL | Controller | Action | Tham số (id) |
| :--- | :--- | :--- | :--- |
| `/Product/Details/5` | `ProductController` | `Details` | `5` |
| `/Product` | `ProductController` | `Index` | `null` |
| `/` | `HomeController` | `Index` | `null` |

### 1.2. Định tuyến bằng thuộc tính (Attribute Routing)
Ngoài cách cấu hình tập trung, lập trình viên có thể sử dụng các thuộc tính ngay trên đầu Controller hoặc Action để tùy biến URL:

```csharp
[Route("danh-muc-san-pham")]
public class CategoryController : Controller {
    [Route("chi-tiet/{slug}")]
    public IActionResult Details(string slug) { ... }
}
```
URL truy cập lúc này sẽ là: `/danh-muc-san-pham/chi-tiet/dien-thoai-iphone`.

---

## 2. Quy trình xử lý yêu cầu (Request Lifecycle)

Khi một yêu cầu được gửi đến hệ thống, quy trình ánh xạ diễn ra như sau:

1.  **Tiếp nhận Request:** Middleware định tuyến phân tích URL dựa trên các Route đã đăng ký.
2.  **Lựa chọn Controller:** Hệ thống khởi tạo một thực thể (instance) của Controller tương ứng.
3.  **Ràng buộc tham số (Parameter Binding):** Hệ thống tự động trích xuất dữ liệu từ URL, Query String hoặc Form Data để truyền vào các tham số của Action method.
4.  **Thực thi Action:** Logic bên trong Action được thực hiện (ví dụ: truy vấn dữ liệu từ MySQL).
5.  **Trả về kết quả (ActionResult):** Action kết thúc bằng việc trả về một kết quả, thường là `View()`.

---

## 3. Tương tác giữa Controller và View

Controller đóng vai trò trung gian, điều phối dữ liệu từ tầng Model sang tầng View để hiển thị.

### 3.1. Phương thức trả về View
Khi gọi hàm `return View();`, hệ thống sẽ tự động tìm kiếm tập tin giao diện theo quy tắc:
`Views/[Tên Controller]/[Tên Action].cshtml`

### 3.2. Truyền dữ liệu sang View
Có ba phương thức chính để chuyển dữ liệu từ Controller sang View:

#### A. Sử dụng ViewModel (Khuyên dùng trong môi trường chuyên nghiệp)
Đây là phương thức truyền dữ liệu "Strongly Typed", giúp kiểm soát lỗi ngay tại thời điểm biên dịch.
*   **Tại Controller:**
    ```csharp
    var product = _context.Products.Find(id);
    return View(product); // Truyền trực tiếp đối tượng product sang View
    ```
*   **Tại View (`Details.cshtml`):**
    ```razor
    @model ManualMvcMySql.Models.Product
    <h1>@Model.Name</h1>
    <p>Giá: @Model.Price</p>
    ```

#### B. Sử dụng ViewData và ViewBag (Dành cho dữ liệu phụ trợ)
Dùng để truyền các mẩu dữ liệu nhỏ, không cấu trúc (như tiêu đề trang, thông báo thành công).
*   **ViewData:** Sử dụng cấu trúc Key-Value (Dictionary).
    *   Controller: `ViewData["Title"] = "Danh sách sản phẩm";`
    *   View: `<title>@ViewData["Title"]</title>`
*   **ViewBag:** Một đối tượng động (dynamic), bao bọc quanh ViewData.
    *   Controller: `ViewBag.Message = "Cập nhật thành công!";`
    *   View: `<p>@ViewBag.Message</p>`

---

## 4. Razor View Engine và Render quy trình

Sau khi Controller chuyển dữ liệu cho View, **Razor View Engine** sẽ thực hiện các nhiệm vụ sau:

1.  Đọc tập tin `.cshtml`.
2.  Phát hiện các ký tự `@` để thực thi mã C#.
3.  Kết hợp dữ liệu từ Model vào các vị trí được chỉ định trong mã HTML.
4.  Kiểm tra tập tin `_ViewStart.cshtml` để xác định Layout chung (Header, Footer).
5.  Kết hợp tất cả thành một tài liệu HTML thuần túy và gửi về trình duyệt của người dùng qua giao thức HTTP Response.

Sự phân tách rõ rệt này đảm bảo rằng logic xử lý dữ liệu (C#) và logic hiển thị (HTML/CSS) không bị chồng chéo, giúp hệ thống đạt được tính mô-đun hóa cao.