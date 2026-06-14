Trong phát triển ứng dụng Web với **.NET 8 MVC**, bảo mật là một tiến trình xuyên suốt từ khâu thiết kế đến triển khai. Dưới đây là các lỗ hổng bảo mật phổ biến nhất (dựa trên tiêu chuẩn OWASP) và các biện pháp phòng chống tương ứng trong hệ sinh thái .NET.

---

# CÁC VẤN ĐỀ BẢO MẬT TRỌNG YẾU VÀ BIỆN PHÁP PHÒNG TRÁNH TRONG .NET 8

## 1. SQL Injection (Tấn công tiêm nhiễm SQL)
Đây là lỗ hổng xảy ra khi dữ liệu từ người dùng được nối trực tiếp vào chuỗi truy vấn SQL, cho phép kẻ tấn công thao túng cơ sở dữ liệu.

*   **Rủi ro:** Lộ dữ liệu nhạy cảm, bị xóa bảng hoặc chiếm quyền quản trị DB.
*   **Phòng tránh:**
    *   Luôn sử dụng **Entity Framework Core (LINQ)** vì EF Core mặc định sử dụng *Parameterized Queries*.
    *   Tránh sử dụng các hàm thực thi SQL thuần như `FromSqlRaw` với chuỗi nối thủ công.
*   **Code mẫu an toàn:**
    ```csharp
    // AN TOÀN: Tham số được tách biệt hoàn toàn
    var user = _context.Users.FromSqlRaw("SELECT * FROM Users WHERE Email = {0}", userEmail).FirstOrDefault();
    ```

## 2. Cross-Site Scripting (XSS - Tấn công thực thi kịch bản liên trang)
XSS xảy ra khi ứng dụng chèn dữ liệu không an toàn vào trang web, cho phép kẻ tấn công thực thi mã JavaScript trên trình duyệt của người dùng khác.

*   **Rủi ro:** Đánh cắp Cookie, Session Token hoặc điều hướng người dùng đến trang web độc hại.
*   **Phòng tránh:**
    *   Mặc định, Razor Engine đã tự động **HTML Encode** tất cả dữ liệu (biến các ký tự `< >` thành `&lt; &gt;`).
    *   Hạn chế tối đa việc sử dụng `@Html.Raw()`.
    *   Nếu bắt buộc phải dùng nội dung HTML (như từ CKEditor), hãy sử dụng thư viện **HtmlSanitizer** để lọc mã độc.
*   **Code mẫu an toàn:**
    ```csharp
    var sanitizer = new HtmlSanitizer();
    var cleanHtml = sanitizer.Sanitize(userInputHtml); // Loại bỏ thẻ <script>, sự kiện onclick...
    ```

## 3. Cross-Site Request Forgery (CSRF - Tấn công giả mạo yêu cầu)
Kẻ tấn công lừa trình duyệt của người dùng gửi một yêu cầu giả mạo đến ứng dụng khi người dùng đã đăng nhập.

*   **Rủi ro:** Thay đổi mật khẩu, thực hiện giao dịch tài chính mà người dùng không biết.
*   **Phòng tránh:**
    *   Sử dụng **Anti-forgery tokens**. Trong .NET MVC, tất cả các thẻ `<form>` sử dụng Tag Helpers (`asp-action`) đều tự động được chèn một token ẩn.
    *   Sử dụng thuộc tính `[ValidateAntiForgeryToken]` trên các Action xử lý POST/PUT/DELETE.
*   **Code mẫu:**
    ```csharp
    [HttpPost]
    [ValidateAntiForgeryToken] // Bắt buộc kiểm tra token hợp lệ
    public IActionResult UpdateProfile(UserModel model) { ... }
    ```

## 4. Mass Assignment / Over-posting (Tấn công gán dữ liệu hàng loạt)
Xảy ra khi lập trình viên sử dụng trực tiếp đối tượng Entity (Database Model) làm tham số trong Controller Action. Kẻ tấn công có thể gửi thêm các trường dữ liệu không được phép (ví dụ: `IsAdmin=true`) qua Form.

*   **Rủi ro:** Người dùng bình thường có thể tự nâng quyền hoặc thay đổi các trường dữ liệu nhạy cảm.
*   **Phòng tránh:**
    *   Sử dụng **ViewModel** hoặc **DTO (Data Transfer Object)** thay vì đưa trực tiếp Entity vào Action.
    *   Chỉ nhận những trường cần thiết thông qua thuộc tính `[Bind]`.
*   **Code mẫu:**
    ```csharp
    // CHUYÊN NGHIỆP: Chỉ nhận các trường trong ProductViewModel
    [HttpPost]
    public IActionResult Create(ProductViewModel model) { ... }
    ```

### Tổng kết nguyên tắc "Vàng":
1.  **Không bao giờ tin tưởng dữ liệu từ người dùng:** Mọi dữ liệu từ Client gửi lên đều phải được Validate và Sanitize.
2.  **Nguyên tắc đặc quyền tối thiểu:** Chỉ cấp quyền vừa đủ cho ứng dụng và người dùng để thực hiện công việc.
3.  **Luôn cập nhật:** Giữ cho các NuGet Packages và .NET SDK luôn ở phiên bản mới nhất để nhận các bản vá bảo mật.