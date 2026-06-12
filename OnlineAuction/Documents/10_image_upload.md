Tài liệu dưới đây hướng dẫn quy trình tích hợp dịch vụ **Cloudinary** để quản lý hình ảnh trong luồng tạo mới sản phẩm. Việc sử dụng Cloudinary giúp tối ưu hóa tài nguyên máy chủ, hỗ trợ CDN và tự động xử lý kích thước hình ảnh.

---

# HƯỚNG DẪN TÍCH HỢP CLOUDINARY ĐỂ TẢI ẢNH TRONG .NET 8 MVC

## 1. Chuẩn bị cấu hình Cloudinary
1.  Đăng ký tài khoản tại [Cloudinary](https://cloudinary.com/).
2.  Lấy các thông số tại trang Dashboard: **Cloud Name**, **API Key**, và **API Secret**.
3.  Khai báo thông số trong tập tin `appsettings.json`:

```json
{
  "CloudinarySettings": {
    "CloudName": "your_cloud_name",
    "ApiKey": "your_api_key",
    "ApiSecret": "your_api_secret"
  }
}
```

---

## 2. Cài đặt thư viện và Khởi tạo Helper
1.  Cài đặt NuGet package: `CloudinaryDotNet`.
2.  Tạo một lớp Helper để ánh xạ cấu hình từ `appsettings.json`.
    *   Tạo file `Helpers/CloudinarySettings.cs`:
    ```csharp
    public class CloudinarySettings {
        public string CloudName { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ApiSecret { get; set; } = string.Empty;
    }
    ```

---

## 3. Xây dựng Dịch vụ Tải ảnh (Photo Service)
Để mã nguồn chuyên nghiệp và dễ tái sử dụng, chúng ta tách logic tải ảnh thành một Service riêng.

1.  Tạo Interface `Interfaces/IPhotoService.cs`:
    ```csharp
    using CloudinaryDotNet.Actions;
    public interface IPhotoService {
        Task<ImageUploadResult> AddPhotoAsync(IFormFile file);
    }
    ```

2.  Triển khai Service `Services/PhotoService.cs`:
    ```csharp
    using CloudinaryDotNet;
    using CloudinaryDotNet.Actions;
    using Microsoft.Extensions.Options;
    using YourProject.Helpers;

    public class PhotoService : IPhotoService {
        private readonly Cloudinary _cloudinary;

        public PhotoService(IOptions<CloudinarySettings> config) {
            var acc = new Account(config.Value.CloudName, config.Value.ApiKey, config.Value.ApiSecret);
            _cloudinary = new Cloudinary(acc);
        }

        public async Task<ImageUploadResult> AddPhotoAsync(IFormFile file) {
            var uploadResult = new ImageUploadResult();
            if (file.Length > 0) {
                using var stream = file.OpenReadStream();
                var uploadParams = new ImageUploadParams {
                    File = new FileDescription(file.FileName, stream),
                    Transformation = new Transformation().Height(500).Width(500).Crop("fill") // Tự động resize
                };
                uploadResult = await _cloudinary.UploadAsync(uploadParams);
            }
            return uploadResult;
        }
    }
    ```

---

## 4. Đăng ký Dịch vụ trong Program.cs
Thêm cấu hình Cloudinary vào DI Container:

```csharp
builder.Services.Configure<CloudinarySettings>(builder.Configuration.GetSection("CloudinarySettings"));
builder.Services.AddScoped<IPhotoService, PhotoService>();
```

---

## 5. Cập nhật Model và xử lý tại Controller

### 5.1. Model Sản phẩm
Trong Database, chúng ta chỉ lưu đường dẫn ảnh (`string`). Trong Model xử lý dữ liệu từ Form, chúng ta thêm thuộc tính `IFormFile`.

```csharp
public class Product {
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; } // Lưu vào MySQL

    [NotMapped] // Không tạo cột này trong Database
    public IFormFile? ImageFile { get; set; } 
}
```

### 5.2. Xử lý tại Controller
Chỉnh sửa Action `Create` để tiếp nhận tập tin và tải lên Cloudinary.

```csharp
[HttpPost]
public async Task<IActionResult> Create(Product product) {
    if (ModelState.IsValid) {
        if (product.ImageFile != null) {
            var result = await _photoService.AddPhotoAsync(product.ImageFile);
            if (result.Error != null) {
                ModelState.AddModelError("ImageFile", "Tải ảnh thất bại.");
                return View(product);
            }
            product.ImageUrl = result.SecureUrl.AbsoluteUri; // Lấy URL từ Cloudinary
        }

        _context.Add(product);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
    return View(product);
}
```

---

## 6. Xây dựng Giao diện (View)
Tại file `Create.cshtml`, điều quan trọng nhất là phải thêm thuộc tính `enctype="multipart/form-data"` vào thẻ `<form>` để trình duyệt cho phép gửi dữ liệu tập tin.

```razor
@model YourProject.Models.Product

<form asp-action="Create" method="post" enctype="multipart/form-data">
    <div class="form-group">
        <label>Tên sản phẩm</label>
        <input asp-for="Name" class="form-control" />
    </div>

    <div class="form-group">
        <label>Hình ảnh sản phẩm</label>
        <input asp-for="ImageFile" type="file" class="form-control" />
        <span asp-validation-for="ImageFile" class="text-danger"></span>
    </div>

    <button type="submit" class="btn btn-primary">Lưu sản phẩm</button>
</form>
```

---

## 7. Hiển thị ảnh tại trang Index
Tại trang danh sách, bạn chỉ cần gọi thuộc tính `ImageUrl` để hiển thị ảnh từ CDN của Cloudinary.

```razor
<td>
    <img src="@item.ImageUrl" alt="@item.Name" style="width:100px; height:auto;" />
</td>
```

## Tổng kết các điểm lưu ý:
1.  **Enctype:** Thiếu `enctype="multipart/form-data"` sẽ khiến thuộc tính `IFormFile` luôn bị null tại Controller.
2.  **Bất đồng bộ (Async):** Việc tải ảnh lên Cloudinary là một tác vụ I/O, do đó luôn phải sử dụng `await` để tránh làm treo ứng dụng.
3.  **Bảo mật:** Không nên lưu API Secret trực tiếp trong mã nguồn nếu đưa lên GitHub (sử dụng *User Secrets* hoặc *Environment Variables* trong thực tế).
4.  **Transformation:** Cloudinary hỗ trợ xử lý ảnh ngay khi tải lên (như cắt góc, nén dung lượng, resize), giúp giảm tải cho thiết bị người dùng khi hiển thị.