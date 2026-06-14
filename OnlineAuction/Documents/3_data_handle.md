# CƠ CHẾ TIẾP NHẬN VÀ TRUYỀN TẢI DỮ LIỆU TRONG ASP.NET CORE MVC

Trong .NET 8, quá trình tự động ánh xạ dữ liệu từ yêu cầu HTTP vào các tham số của Action được gọi là **Model Binding**.

## 1. Truyền dữ liệu từ Client lên Controller

Dữ liệu có thể được gửi lên từ Client thông qua ba phương thức phổ biến sau:

### 1.1. Tham số lộ trình (Path Variable / Route Parameter)
Dữ liệu được nhúng trực tiếp vào cấu trúc URL, thường dùng để định danh một đối tượng cụ thể.
*   **Cấu trúc URL:** `/Product/Edit/101`
*   **Cấu hình tại Controller:**
    ```csharp
    // Hệ thống tự hiểu 'id' lấy từ phân đoạn cuối của URL theo cấu hình Route mặc định
    public IActionResult Edit(int id) 
    {
        // id sẽ có giá trị là 101
        return View();
    }
    ```

### 1.2. Chuỗi truy vấn (Query String)
Dữ liệu nằm sau dấu `?` trong URL, thường dùng cho các tác vụ lọc, tìm kiếm hoặc phân trang.
*   **Cấu trúc URL:** `/Product/Search?name=iphone&category=mobile`
*   **Cấu hình tại Controller:**
    ```csharp
    public IActionResult Search(string name, string category)
    {
        // name = "iphone", category = "mobile"
        return View();
    }
    ```

### 1.3. Dữ liệu biểu mẫu (Form Data)
Dữ liệu được gửi ngầm qua phương thức HTTP POST, thường dùng khi tạo mới hoặc cập nhật đối tượng để đảm bảo bảo mật và truyền tải được dữ liệu lớn.
*   **Cấu trúc View (HTML):**
    ```html
    <form asp-action="Create" method="post">
        <input name="Name" />
        <input name="Price" />
        <button type="submit">Gửi</button>
    </form>
    ```
*   **Cấu hình tại Controller:**
    Lập trình viên có thể tiếp nhận từng tham số hoặc ánh xạ trực tiếp vào một đối tượng (Model Binding).
    ```csharp
    [HttpPost]
    public IActionResult Create(Product product)
    {
        // Các thuộc tính của 'product' sẽ tự động khớp với các thẻ 'name' trong form
        return View("Details", product);
    }
    ```

---

## 2. Truyền dữ liệu từ Controller về View

Sau khi xử lý, Controller cần gửi dữ liệu ngược lại cho View để hiển thị.

### 2.1. Sử dụng Strongly Typed Model (Khuyên dùng)
Đây là cách an toàn nhất, cho phép kiểm tra kiểu dữ liệu ngay khi viết code.
*   **Controller:**
    ```csharp
    public IActionResult Details()
    {
        var product = new Product { Name = "iPhone 15", Price = 1000 };
        return View(product); // Truyền model vào View
    }
    ```

### 2.2. Sử dụng ViewData và ViewBag
Dùng cho các thông tin bổ trợ không nằm trong Model chính.
*   **Controller:**
    ```csharp
    ViewBag.PageTitle = "Chi tiết sản phẩm";
    ViewData["Timestamp"] = DateTime.Now;
    ```

---

## 3. Hiển thị dữ liệu tại View (Razor Syntax)

Tại lớp giao diện (`.cshtml`), dữ liệu được trích xuất và hiển thị thông qua cú pháp Razor.

### 3.1. Khai báo và sử dụng Model
Ở dòng đầu tiên của tập tin View, cần khai báo kiểu dữ liệu của Model nhận được.

```razor
@model ManualMvcMySql.Models.Product

<h2>@ViewBag.PageTitle</h2>

<div>
    <p>Tên sản phẩm: <strong>@Model.Name</strong></p>
    <p>Giá: @Model.Price.ToString("C")</p>
</div>

<p>Thời gian truy cập: @ViewData["Timestamp"]</p>
```

---

## 4. Tổng kết quy trình luân chuyển dữ liệu

| Thành phần | Vai trò | Công nghệ/Cú pháp |
| :--- | :--- | :--- |
| **Client** | Gửi yêu cầu | URL (Route/Query) hoặc Form POST |
| **Model Binder** | Ánh xạ dữ liệu | Tự động khớp tên tham số (Case-insensitive) |
| **Controller Action** | Xử lý nghiệp vụ | Tiếp nhận tham số, truy vấn DB, đóng gói Model |
| **View Engine** | Kết xuất giao diện | Razor Syntax (`@Model`, `@ViewBag`) |
| **Browser** | Hiển thị kết quả | Nhận HTML thuần túy từ Server |

**Lưu ý chuyên môn:** Khi đặt tên cho các trường trong biểu mẫu (`input name="..."`) hoặc các tham số trên URL, cần đảm bảo chúng khớp hoàn toàn với tên thuộc tính của Class Model hoặc tên tham số trong Action để hệ thống **Model Binding** có thể hoạt động chính xác.