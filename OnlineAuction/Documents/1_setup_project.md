Khởi tạo và cấu trúc hóa dự án **ASP.NET Core 8 (MVC)** sử dụng **JetBrains Rider**.

---

# HƯỚNG DẪN KHỞI TẠO VÀ CẤU TRÚC DỰ ÁN .NET 8 MVC

## 1. Khởi tạo dự án
Để bắt đầu, thực hiện các bước thiết lập cơ bản trên JetBrains Rider:

1.  Mở JetBrains Rider, chọn **New Solution**.
2.  Tại danh sách mẫu (Templates), chọn **ASP.NET Core Web App (Model-View-Controller)**.
3.  Thiết lập các thông số kỹ thuật:
    *   **Solution Name:** Tên giải pháp (Ví dụ: `ProductManagementSystem`).
    *   **SDK:** .NET 8.0 (Phiên bản hỗ trợ dài hạn mới nhất).
    *   **Authentication:** None (Đối với các dự án bắt đầu từ cơ bản).
4.  Nhấn **Create** để hệ thống tự động sinh mã nguồn cơ sở.

---

## 2. Cấu trúc thư mục

Một dự án .NET MVC tiêu chuẩn được tổ chức theo các phân vùng chức năng riêng biệt nhằm đảm bảo tính dễ bảo trì (Maintainability):

*   **`Controllers/` (Tầng điều khiển):** 
    *   Chứa các lớp xử lý logic điều hướng. 
    *   Nhiệm vụ: Tiếp nhận yêu cầu từ người dùng thông qua giao thức HTTP, tương tác với dữ liệu (Model) và quyết định hiển thị giao diện (View) tương ứng.
*   **`Models/` (Tầng dữ liệu):** 
    *   Chứa các lớp (Class) định nghĩa cấu trúc dữ liệu và các quy tắc nghiệp vụ (Business Logic). 
    *   Đây là đại diện của các bảng trong cơ sở dữ liệu khi làm việc với ORM (Entity Framework Core).
*   **`Views/` (Tầng hiển thị):** 
    *   Chứa các tập tin định dạng `.cshtml` (kết hợp giữa HTML và cú pháp C# Razor).
    *   Nhiệm vụ: Chuyển đổi dữ liệu từ Controller thành giao diện trực quan cho người dùng cuối.
*   **`wwwroot/` (Tài nguyên tĩnh):** 
    *   Thư mục công khai duy nhất chứa các tập tin không thay đổi như CSS, JavaScript, hình ảnh và các thư viện phía Front-end (Bootstrap, jQuery).
*   **`Properties/`:** 
    *   Chứa tập tin `launchSettings.json`, định nghĩa các môi trường thực thi (Development, Staging, Production) và cấu hình cổng mạng (Port) của máy chủ Web.

---

## 3. Các thành phần cấu hình cốt lõi

### 3.1. Tập tin `Program.cs`
Đây là điểm nhập (Entry Point) của toàn bộ ứng dụng. Vai trò của `Program.cs` tập trung vào hai phần:
*   **Dependency Injection (DI) Container:** Nơi đăng ký các dịch vụ hệ thống như Cơ sở dữ liệu, Identity, hoặc các dịch vụ tùy chỉnh.
*   **Middleware Pipeline:** Thiết lập các bộ lọc xử lý yêu cầu HTTP (như xử lý lỗi, bảo mật HTTPS, định tuyến tĩnh).

### 3.2. Tập tin `appsettings.json`
Tập tin cấu hình dưới định dạng JSON, lưu trữ các tham số biến thiên của ứng dụng như:
*   Chuỗi kết nối cơ sở dữ liệu (Connection Strings).
*   Cấu hình mức độ ghi nhật ký hệ thống (Logging levels).
*   Các khóa bí mật của bên thứ ba (API Keys).

---

## 4. Quản lý thư viện (Package Management)

Trong hệ sinh thái .NET, việc quản lý các thư viện mở rộng được thực hiện thông qua **NuGet**. Đối với dự án làm việc với MySQL, các gói sau là bắt buộc:

1.  **`Pomelo.EntityFrameworkCore.MySql`**: Nhà cung cấp dữ liệu (Provider) chính để Entity Framework Core có thể hiểu và thực thi lệnh trên hệ quản trị cơ sở dữ liệu MySQL.
2.  **`Microsoft.EntityFrameworkCore.Design`**: Cung cấp các công cụ dòng lệnh để thực hiện kỹ thuật Migration (chuyển đổi mã nguồn C# thành cấu trúc bảng trong Database).

**Quy trình cài đặt:** Truy cập tab **NuGet** ở phía dưới trình soạn thảo Rider, tìm kiếm tên gói và nhấn cài đặt vào dự án mục tiêu.

---

## 5. Cơ chế vận hành của mô hình MVC

Ứng dụng hoạt động dựa trên luồng xử lý tuần tự:

1.  **Request:** Trình duyệt gửi một yêu cầu HTTP (ví dụ: `GET /Product/Index`).
2.  **Routing:** Hệ thống định tuyến phân tích yêu cầu và chuyển hướng tới **Controller** tương ứng.
3.  **Action:** Phương thức trong Controller thực thi, gọi dữ liệu từ **Model** (hoặc Database).
4.  **View Engine:** Controller truyền dữ liệu vào **View**. Engine sẽ biên dịch mã C# thành HTML thuần túy.
5.  **Response:** Máy chủ trả về tài liệu HTML hoàn chỉnh cho trình duyệt người dùng.

Tài liệu này cung cấp cái nhìn tổng quan về kiến trúc hệ thống. Việc tuân thủ cấu trúc này giúp dự án đạt được sự ổn định và dễ dàng mở rộng trong tương lai.