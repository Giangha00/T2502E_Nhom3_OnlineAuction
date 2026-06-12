# QUẢN TRỊ THƯ VIỆN VỚI NUGET PACKAGE TRONG .NET 8

## 1. Khái niệm về NuGet Package
**NuGet** là trình quản lý gói (Package Manager) tiêu chuẩn cho .NET. Một "gói" (package) là một tập tin nén có đuôi `.nupkg`, chứa mã nguồn đã được biên dịch (DLL), các tập tin liên quan và một tập tin mô tả (manifest) bao gồm thông tin về phiên bản và các thành phần phụ thuộc (dependencies).

## 2. Cơ chế lưu trữ và quản lý

### 2.1. Lưu trữ trong dự án (Project Level)
Trong .NET 8, thông tin về các thư viện không được lưu trữ trực tiếp mã nguồn trong thư mục dự án. Thay vào đó, chúng được khai báo trong tập tin định nghĩa dự án: **`*.csproj`**.
*   Khi thêm một thư viện, một thẻ `<PackageReference>` sẽ được thêm vào tập tin này.
*   **Ưu điểm:** Giảm dung lượng mã nguồn khi lưu trữ trên các hệ thống quản lý phiên bản như Git.

### 2.2. Cơ chế lưu trữ cục bộ (Global Cache)
Khi một gói được tải xuống, NuGet sẽ lưu trữ tại thư mục bộ nhớ đệm toàn cục trên máy tính:
*   **Windows:** `%userprofile%\.nuget\packages`
*   **Linux/macOS:** `~/.nuget/packages`
Cơ chế này giúp tiết kiệm dung lượng đĩa và thời gian biên dịch, vì nhiều dự án khác nhau có thể cùng sử dụng một bản cài đặt duy nhất từ bộ nhớ đệm này.

---

## 3. Thao tác với NuGet trên JetBrains Rider

### 3.1. Thêm mới thư viện (Install)
1.  Mở cửa sổ công cụ **NuGet** (thường nằm ở dưới cùng của giao diện hoặc vào menu **View -> Tool Windows -> NuGet**).
2.  Tại tab **Packages**, nhập tên thư viện cần tìm (Ví dụ: `Newtonsoft.Json`).
3.  Chọn phiên bản phù hợp tại cột bên phải (mặc định là phiên bản ổn định mới nhất).
4.  Nhấn biểu tượng dấu cộng (**+**) hoặc nút **Install** để tích hợp vào dự án.

### 3.2. Gỡ bỏ thư viện (Remove)
1.  Trong cửa sổ NuGet, chuyển sang tab **Installed**.
2.  Chọn thư viện cần loại bỏ khỏi danh sách.
3.  Nhấn biểu tượng dấu trừ (**-**) hoặc nút **Uninstall**. Rider sẽ tự động cập nhật tập tin `.csproj` và xóa bỏ các tham chiếu liên quan.

### 3.3. Cập nhật thư viện (Update)
*   Tại tab **Updates**, hệ thống sẽ liệt kê các thư viện hiện có phiên bản mới hơn. Lập trình viên có thể chọn cập nhật từng gói hoặc toàn bộ để tối ưu hiệu suất và bảo mật.

---

## 4. Quản lý thông qua giao diện dòng lệnh (CLI)

Đối với các môi trường CI/CD hoặc lập trình viên ưu tiên sử dụng terminal, .NET cung cấp các lệnh sau:

*   **Thêm thư viện:**
    ```bash
    dotnet add package [TÊN_PACKAGE]
    # Ví dụ: dotnet add package Pomelo.EntityFrameworkCore.MySql
    ```
*   **Gỡ bỏ thư viện:**
    ```bash
    dotnet remove package [TÊN_PACKAGE]
    ```
*   **Phục hồi thư viện (Restore):**
    Thao tác này tải xuống tất cả các thư viện được khai báo trong `.csproj` (thường dùng khi vừa tải mã nguồn từ Git về).
    ```bash
    dotnet restore
    ```

---

## 5. Các lưu ý quan trọng về Dependencies

### 5.1. Thành phần phụ thuộc bắc cầu (Transitive Dependencies)
Khi cài đặt một gói A, nếu gói A yêu cầu gói B để hoạt động, NuGet sẽ tự động tải cả gói B. Trong JetBrains Rider, người dùng có thể xem cấu trúc cây phụ thuộc này trong mục **Dependencies -> Packages** tại cửa sổ Solution Explorer.

### 5.2. Quản lý phiên bản (Versioning)
NuGet tuân thủ quy tắc **Semantic Versioning (SemVer)**: `Major.Minor.Patch`.
*   **Major:** Có thay đổi lớn, có thể gây lỗi logic cho code cũ.
*   **Minor:** Thêm tính năng mới nhưng vẫn tương thích ngược.
*   **Patch:** Vá lỗi bảo mật hoặc hiệu năng nhỏ.

Việc quản lý chặt chẽ các gói NuGet giúp dự án .NET duy trì tính ổn định, tận dụng được sức mạnh của cộng đồng và tối ưu hóa quy trình phát triển phần mềm chuyên nghiệp.