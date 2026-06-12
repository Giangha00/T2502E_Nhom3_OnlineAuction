Tài liệu dưới đây hướng dẫn chi tiết cách tích hợp **CKEditor 5** (phiên bản hiện đại nhất) vào biểu mẫu trong .NET 8 MVC. CKEditor giúp người dùng nhập liệu nội dung dưới dạng văn bản giàu (Rich Text) như in đậm, in nghiêng, tạo bảng, hoặc chèn danh sách.

---

# TÍCH HỢP CKEDITOR 5 TRONG BIỂU MẪU .NET 8 MVC

## 1. Chuẩn bị Model
Trong lớp Model, thuộc tính nhận dữ liệu từ CKEditor phải là kiểu `string`. Do dữ liệu từ CKEditor gửi lên là mã HTML, thuộc tính này sẽ lưu trữ các thẻ như `<p>`, `<strong>`, `<ul>`...

```csharp
public class Product
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Vui lòng nhập mô tả chi tiết")]
    public string Description { get; set; } = string.Empty;
}
```

---

## 2. Cấu hình giao diện (View)

Để tích hợp CKEditor, chúng ta sử dụng thư viện thông qua CDN để đơn giản hóa việc cài đặt.

### 2.1. Cấu trúc thẻ HTML
Tại file `Create.cshtml` hoặc `Edit.cshtml`, sử dụng thẻ `textarea` và gán cho nó một `id` duy nhất.

```razor
@model YourProject.Models.Product

<form asp-action="Create" method="post">
    <div class="form-group">
        <label asp-for="Description" class="control-label">Mô tả sản phẩm</label>
        
        <!-- Thẻ textarea sẽ được CKEditor thay thế -->
        <textarea asp-for="Description" id="editor" class="form-control"></textarea>
        
        <span asp-validation-for="Description" class="text-danger"></span>
    </div>

    <button type="submit" class="btn btn-primary">Lưu sản phẩm</button>
</form>
```

### 2.2. Nhúng thư viện và Khởi tạo JavaScript
Sử dụng khối `@section Scripts` để nhúng mã điều khiển CKEditor.

```razor
@section Scripts {
    <!-- Nhúng thư viện CKEditor 5 từ CDN -->
    <script src="https://cdn.ckeditor.com/ckeditor5/41.0.0/classic/ckeditor.js"></script>

    <script>
        ClassicEditor
            .create(document.querySelector('#editor'), {
                // Cấu hình các công cụ hiển thị trên thanh toolbar (tùy chọn)
                toolbar: ['heading', '|', 'bold', 'italic', 'link', 'bulletedList', 'numberedList', 'blockQuote', 'insertTable', 'undo', 'redo']
            })
            .then(editor => {
                console.log('Editor was initialized', editor);
            })
            .catch(error => {
                console.error(error);
            });
    </script>
    
    <!-- Đừng quên nhúng script validation mặc định của .NET -->
    <partial name="_ValidationScriptsPartial" />
}
```

---

## 3. Xử lý tại Controller
Tại Controller, việc tiếp nhận dữ liệu từ CKEditor không khác gì một ô nhập liệu văn bản thông thường. .NET 8 sẽ tự động ánh xạ (Bind) mã HTML từ Form vào thuộc tính `Description`.

```csharp
[HttpPost]
public async Task<IActionResult> Create(Product product)
{
    if (ModelState.IsValid)
    {
        _context.Add(product);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    return View(product);
}
```

---

## 4. Hiển thị nội dung Rich Text ra View
Đây là bước quan trọng. Theo mặc định, Razor sẽ mã hóa (encode) tất cả các thẻ HTML để chống tấn công XSS. Nếu bạn chỉ dùng `@Model.Description`, trình duyệt sẽ hiển thị cả các thẻ `<p>`, `<strong>`.

Để hiển thị đúng định dạng văn bản, bạn phải sử dụng **`@Html.Raw()`**.

*Trang `Details.cshtml` hoặc `Index.cshtml`:*
```razor
<div class="product-description">
    <h4>Mô tả chi tiết:</h4>
    <!-- Hiển thị nội dung HTML mà không bị mã hóa -->
    @Html.Raw(Model.Description)
</div>
```

---

## 5. Các lưu ý quan trọng về bảo mật và hệ thống

### 5.1. Ngăn chặn tấn công XSS
Việc sử dụng `@Html.Raw()` và cho phép lưu mã HTML vào Database tiềm ẩn rủi ro về bảo mật (người dùng có thể chèn các thẻ `<script>` độc hại).
*   **Giải pháp chuyên nghiệp:** Sử dụng thư viện **Ganss.XSS (HtmlSanitizer)** để làm sạch mã HTML ở Controller trước khi lưu vào Database.

```csharp
// Cài đặt NuGet: HtmlSanitizer
var sanitizer = new HtmlSanitizer();
product.Description = sanitizer.Sanitize(product.Description);
```

### 5.2. Khắc phục lỗi Validation
Đôi khi jQuery Validation có thể báo lỗi ô nhập liệu trống mặc dù bạn đã nhập dữ liệu vào CKEditor. Điều này xảy ra do CKEditor cập nhật dữ liệu chậm hơn một nhịp so với sự kiện submit của Form.
*   **Khắc phục:** Thêm đoạn mã nhỏ sau vào script khởi tạo để ép CKEditor cập nhật dữ liệu vào `textarea` ngay khi có thay đổi:

```javascript
editor.model.document.on('change:data', () => {
    editor.updateSourceElement();
});
```

### 5.3. Lưu trữ ảnh trong CKEditor
Mặc định, CKEditor 5 hỗ trợ tải ảnh qua Base64 (làm Database rất nặng) hoặc qua một Adapter riêng. Để chuyên nghiệp, bạn nên kết hợp CKEditor với luồng **Cloudinary** đã hướng dẫn trước đó bằng cách viết một `UploadAdapter` tùy chỉnh trong JavaScript.

Việc tích hợp CKEditor giúp ứng dụng .NET 8 MVC của bạn có khả năng quản trị nội dung chuyên nghiệp, đáp ứng tốt nhu cầu của các hệ thống CMS hoặc thương mại điện tử.