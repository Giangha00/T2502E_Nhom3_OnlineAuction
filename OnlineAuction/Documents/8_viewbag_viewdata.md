Trong kiến trúc ASP.NET Core MVC, việc luân chuyển dữ liệu từ Controller ra View là một tác vụ cơ bản nhưng đòi hỏi sự lựa chọn kỹ thuật phù hợp để đảm bảo tính hiệu suất và dễ bảo trì.

---

### 1. Strongly Typed Model (Truyền dữ liệu có kiểu tường minh)
Đây là phương thức chuyên nghiệp và được khuyến khích sử dụng nhất trong .NET. Bạn truyền trực tiếp một đối tượng (hoặc một danh sách đối tượng) từ Action vào View thông qua hàm `View()`.

*   **Controller:**
    ```csharp
    public IActionResult Details()
    {
        var product = new Product { Name = "Laptop Dell", Price = 1500 };
        return View(product); // Truyền model trực tiếp vào View
    }
    ```
*   **View (`Details.cshtml`):**
    ```razor
    @model YourProject.Models.Product

    <h1>@Model.Name</h1>
    <p>Giá: @Model.Price</p>
    ```

---

### 2. ViewData
`ViewData` là một từ điển (Dictionary) lưu trữ dữ liệu dưới dạng `Key-Value`. Dữ liệu được lưu trữ kiểu `object`, nên khi trích xuất tại View, bạn thường phải ép kiểu (casting) nếu muốn thực hiện các phép toán phức tạp.

*   **Controller:**
    ```csharp
    public IActionResult Index()
    {
        ViewData["Message"] = "Chào mừng bạn đến với hệ thống!";
        ViewData["TotalItems"] = 10;
        return View();
    }
    ```
*   **View:**
    ```razor
    <h3>@ViewData["Message"]</h3>
    <p>Số lượng: @ViewData["TotalItems"]</p>
    ```

---

### 3. ViewBag
`ViewBag` là một lớp bọc (wrapper) động quanh `ViewData`. Nó sử dụng tính năng `dynamic` của C#, cho phép bạn tạo các thuộc tính tùy ý mà không cần khai báo trước.

*   **Controller:**
    ```csharp
    public IActionResult Index()
    {
        ViewBag.PageTitle = "Trang chủ";
        ViewBag.CurrentDate = DateTime.Now;
        return View();
    }
    ```
*   **View:**
    ```razor
    <h1>@ViewBag.PageTitle</h1>
    <p>Ngày hôm nay: @ViewBag.CurrentDate</p>
    ```

---

### 4. TempData (Dữ liệu dùng một lần / Flash Data)
`TempData` cũng là một từ điển giống `ViewData`, nhưng nó có khả năng **duy trì dữ liệu qua một yêu cầu HTTP kế tiếp (Redirect)**. Sau khi dữ liệu được đọc ở yêu cầu mới, nó sẽ tự động bị xóa bỏ.

*   **Controller:**
    ```csharp
    [HttpPost]
    public IActionResult Create(Product p)
    {
        // Xử lý lưu database thành công
        TempData["SuccessMessage"] = "Thêm sản phẩm thành công!";
        return RedirectToAction("Index"); // Chuyển hướng sang trang Index
    }

    public IActionResult Index()
    {
        return View();
    }
    ```
*   **View (`Index.cshtml`):**
    ```razor
    @if (TempData["SuccessMessage"] != null)
    {
        <div class="alert alert-success">@TempData["SuccessMessage"]</div>
    }
    ```

---

### Bảng so sánh các phương thức truyền dữ liệu

| Tiêu chí | Strongly Typed Model | ViewData | ViewBag | TempData |
| :--- | :--- | :--- | :--- | :--- |
| **Kiểu dữ liệu** | Tường minh (Strongly Typed) | Dictionary (Key-Value) | Dynamic | Dictionary (Key-Value) |
| **Kiểm tra lỗi** | Ngay khi biên dịch (Compile-time) | Khi chạy (Runtime) | Khi chạy (Runtime) | Khi chạy (Runtime) |
| **IntelliSense** | Có hỗ trợ đầy đủ | Không | Không | Không |
| **Thời gian sống** | Trong yêu cầu hiện tại | Trong yêu cầu hiện tại | Trong yêu cầu hiện tại | Qua được một lần Redirect |
| **Hiệu suất** | Tốt nhất | Trung bình (do ép kiểu) | Trung bình (do dynamic) | Trung bình (lưu trong Session/Cookie) |
| **Trường hợp dùng** | Dữ liệu chính của trang (Danh sách, Chi tiết) | Dữ liệu phụ (Tiêu đề, thông tin Layout) | Dữ liệu phụ (giống ViewData) | Thông báo trạng thái (Flash messages) |

---

### Tổng kết và Lời khuyên chuyên môn:

1.  **Ưu tiên dùng @model (Strongly Typed Model):** Luôn sử dụng cách này cho dữ liệu nghiệp vụ chính để tận dụng khả năng bắt lỗi của trình biên dịch và sự hỗ trợ từ IntelliSense của Rider.
2.  **ViewData và ViewBag:** Thực chất chúng dùng chung một bộ nhớ đệm. `ViewBag` viết ngắn gọn hơn nhưng `ViewData` an toàn hơn khi kiểm tra giá trị null. Chỉ nên dùng cho dữ liệu phụ (như Title trang, Breadcrumbs).
3.  **TempData:** Chỉ sử dụng khi bạn cần truyền thông báo (Success/Error) giữa hai Action sau khi thực hiện `RedirectToAction`. Nếu bạn chỉ `return View()`, hãy dùng `ViewBag` thay vì `TempData`.