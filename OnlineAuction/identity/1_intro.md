# GIỚI THIỆU VỀ ASP.NET CORE IDENTITY FRAMEWORK

## 1. ASP.NET Core Identity là gì?
**ASP.NET Core Identity** là một hệ thống quản lý thành viên (membership system) hoàn chỉnh được Microsoft xây dựng sẵn cho các ứng dụng ASP.NET Core. Nó cho phép bạn thêm chức năng đăng nhập, đăng ký, quản lý người dùng, phân quyền và bảo mật vào ứng dụng một cách nhanh chóng.

Thay vì tự xây dựng bảng dữ liệu người dùng, tự viết logic mã hóa mật khẩu hay quản lý Session/Cookie, bạn sử dụng Identity để kế thừa các tiêu chuẩn bảo mật cao nhất hiện nay.

## 2. Các tính năng cốt lõi
Identity cung cấp một bộ tính năng cực kỳ mạnh mẽ:
*   **Quản lý người dùng (User Management):** Đăng ký, đăng nhập, đăng xuất, đổi mật khẩu, quên mật khẩu.
*   **Xác thực (Authentication):** Hỗ trợ xác thực dựa trên Cookie hoặc Token (JWT).
*   **Phân quyền (Authorization):**
    *   *Role-based:* Phân quyền theo vai trò (Admin, Editor, User).
    *   *Claim-based:* Phân quyền dựa trên đặc điểm cụ thể (Ví dụ: "Người dùng có tuổi > 18").
*   **Xác thực hai yếu tố (Two-Factor Authentication - 2FA):** Hỗ trợ gửi mã qua Email hoặc SMS.
*   **Đăng nhập bên thứ ba (External Logins):** Tích hợp dễ dàng với Google, Facebook, Microsoft, GitHub qua giao thức OAuth2/OpenID Connect.
*   **Bảo mật nâng cao:** Tự động xử lý mã hóa mật khẩu (Hashing), chống tấn công Brute Force (Account Lockout).

---

## 3. Các thành phần quản lý chính (The Managers)
Identity hoạt động thông qua 3 lớp dịch vụ chính mà bạn sẽ thường xuyên sử dụng trong Controller:

1.  **UserManager\<TUser\>:** Dịch vụ chính để quản lý người dùng trong cơ sở dữ liệu (Tạo mới, tìm kiếm, kiểm tra mật khẩu, cập nhật thông tin).
2.  **SignInManager\<TUser\>:** Chịu trách nhiệm thực hiện các tác vụ đăng nhập và đăng xuất (Xử lý Cookie, kiểm tra trạng thái đăng nhập).
3.  **RoleManager\<TRole\>:** Dịch vụ quản lý các vai trò/nhóm người dùng (Tạo role, xóa role, gán role cho người dùng).

---

## 4. Cấu trúc Cơ sở dữ liệu (Database Schema)
Khi tích hợp Identity vào dự án, nó sẽ tự động tạo ra một bộ bảng trong database (thông qua Entity Framework Core) để lưu trữ thông tin. Các bảng mặc định bao gồm:
*   `AspNetUsers`: Lưu thông tin tài khoản (Username, Email, PasswordHash...).
*   `AspNetRoles`: Lưu danh sách các vai trò (Admin, User...).
*   `AspNetUserRoles`: Bảng trung gian nối người dùng với vai trò (N - N).
*   `AspNetUserClaims`: Lưu các đặc điểm bổ sung của người dùng.
*   `AspNetUserLogins`: Lưu thông tin khi người dùng đăng nhập bằng Google/Facebook.

---

## 5. Khả năng tùy biến (Extensibility)
Một điểm mạnh của Identity là khả năng tùy biến rất cao:
*   **Custom User:** Bạn có thể dễ dàng thêm các trường dữ liệu như `FullName`, `Address`, `AvatarUrl` vào lớp người dùng mặc định.
*   **Custom Storage:** Mặc định Identity dùng Entity Framework Core, nhưng bạn có thể cấu hình để nó chạy trên Dapper hoặc các hệ quản trị dữ liệu NoSQL nếu muốn.

---

## 6. Tại sao không nên tự viết hệ thống Login?
Việc tự xây dựng hệ thống quản lý người dùng từ đầu tiềm ẩn rất nhiều rủi ro bảo mật:
*   **Mã hóa sai cách:** Lưu mật khẩu không an toàn.
*   **Lỗi logic:** Quản lý Session/Cookie không chặt chẽ dẫn đến bị tấn công đánh cắp phiên làm việc.
*   **Thiếu tính năng:** Rất tốn thời gian để tự viết chức năng reset mật khẩu qua email hoặc đăng nhập Google.
