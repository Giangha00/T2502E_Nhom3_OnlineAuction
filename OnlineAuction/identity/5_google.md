# HƯỚNG DẪN TÍCH HỢP ĐĂNG NHẬP GOOGLE TRONG .NET 8 MVC

## 1. Đăng ký ứng dụng trên Google Cloud Console

Trước khi viết code, bạn cần lấy thông tin định danh từ Google.

1.  Truy cập [Google Cloud Console](https://console.cloud.google.com/).
2.  Tạo một Project mới.
3.  Vào mục **APIs & Services > OAuth consent screen**: Chọn **External** và điền các thông tin bắt buộc.
4.  Vào mục **Credentials > Create Credentials > OAuth client ID**:
    *   **Application type:** Web application.
    *   **Authorized redirect URIs:** Thêm đường dẫn sau (đối với môi trường local):
        `https://localhost:xxxx/signin-google`
        *(Thay `xxxx` bằng cổng port HTTPS của dự án bạn đang chạy trên Rider).*
5.  Lưu lại **Client ID** và **Client Secret**.

---

## 2. Cấu hình dự án .NET 8

### 2.1. Cài đặt thư viện (NuGet)
Mở Terminal tại dự án và chạy lệnh:
```bash
dotnet add package Microsoft.AspNetCore.Authentication.Google
```

### 2.2. Cấu hình bí mật (Appsettings.json)
Lưu trữ thông tin định danh vào tập tin cấu hình:
```json
{
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_CLIENT_ID.apps.googleusercontent.com",
      "ClientSecret": "YOUR_CLIENT_SECRET"
    }
  }
}
```

### 2.3. Đăng ký dịch vụ trong Program.cs
Cấu hình Middleware để hệ thống nhận diện phương thức xác thực Google.

```csharp
var builder = WebApplication.CreateBuilder(args);

// Cấu hình Identity (đã làm ở phần trước)
builder.Services.AddIdentity<AppUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// Đăng ký Google Authentication
builder.Services.AddAuthentication()
    .AddGoogle(options =>
    {
        IConfigurationSection googleAuthNSection = builder.Configuration.GetSection("Authentication:Google");
        options.ClientId = googleAuthNSection["ClientId"];
        options.ClientSecret = googleAuthNSection["ClientSecret"];
    });

builder.Services.AddControllersWithViews();
```

---

## 3. Xử lý Logic tại AccountController

Chúng ta cần hai Action: một để gửi yêu cầu tới Google và một để tiếp nhận kết quả trả về.

```csharp
public class AccountController : Controller
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;

    public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    // 1. Gửi yêu cầu đăng nhập tới Google
    [HttpPost]
    public IActionResult ExternalLogin(string provider, string? returnUrl = null)
    {
        var redirectUrl = Url.Action("ExternalLoginCallback", "Account", new { returnUrl });
        var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    // 2. Tiếp nhận kết quả trả về từ Google
    public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/");

        if (remoteError != null)
        {
            ModelState.AddModelError("", $"Lỗi từ dịch vụ bên thứ ba: {remoteError}");
            return View("Login");
        }

        // Lấy thông tin đăng nhập từ Google trả về
        var info = await _signInManager.GetExternalLoginInfoAsync();
        if (info == null) return RedirectToAction("Login");

        // Đăng nhập nếu tài khoản đã tồn tại và đã liên kết với Google
        var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false);
        
        if (result.Succeeded) return LocalRedirect(returnUrl);

        // Nếu tài khoản chưa có, tiến hành tạo mới
        var email = info.Principal.FindFirstValue(ClaimTypes.Email);
        if (email != null)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    FullName = info.Principal.FindFirstValue(ClaimTypes.Name) ?? email
                };
                await _userManager.CreateAsync(user);
            }

            // Liên kết tài khoản vừa tạo với Google
            await _userManager.AddLoginAsync(user, info);
            await _signInManager.SignInAsync(user, isPersistent: false);
            
            return LocalRedirect(returnUrl);
        }

        return View("Login");
    }
}
```

---

## 4. Cập nhật giao diện Đăng nhập (View)

Thêm nút bấm để người dùng kích hoạt luồng đăng nhập Google.

*Tập tin: `Views/Account/Login.cshtml`*
```razor
<form asp-action="ExternalLogin" asp-route-returnUrl="@Model.ReturnUrl" method="post">
    <button type="submit" name="provider" value="Google" class="btn btn-danger w-100 mb-3">
        <i class="bi bi-google"></i> Đăng nhập bằng Google
    </button>
</form>
```

---

## 5. Giải thích luồng hoạt động chuyên sâu

1.  **ChallengeResult:** Khi bạn nhấn nút, `ChallengeResult` sẽ kích hoạt Middleware xác thực, chuyển hướng trình duyệt của người dùng tới trang đăng nhập của Google.
2.  **Callback (Phản hồi):** Sau khi người dùng đăng nhập thành công trên Google, Google sẽ gửi một mã Token về đường dẫn `/signin-google`. Middleware của .NET sẽ tự động bắt lấy mã này và chuyển đổi thành đối tượng `ExternalLoginInfo`.
3.  **ExternalLoginSignInAsync:** Phương thức này kiểm tra trong bảng `AspNetUserLogins` xem định danh Google này đã được liên kết với tài khoản nào trong hệ thống chưa.
4.  **Tự động tạo tài khoản:** Nếu email từ Google chưa tồn tại trong hệ thống, chúng ta tự động tạo một `AppUser` mới. Việc gọi `AddLoginAsync` là để lưu lại mối liên kết giữa User đó với tài khoản Google, giúp họ có thể đăng nhập bằng Google vào những lần sau.

## Lưu ý về bảo mật
*   **Redirect URI:** Phải khớp chính xác từng ký tự giữa cấu hình trên Google Cloud và URL thực tế của ứng dụng.
*   **HTTPS:** Google yêu cầu chuyển hướng phải qua giao thức HTTPS (ngoại trừ localhost).
*   **Secrets:** Trong thực tế, bạn nên sử dụng **Secret Manager** hoặc **Environment Variables** để lưu `ClientSecret` thay vì viết trực tiếp vào file `appsettings.json` khi đưa lên các kho lưu trữ mã nguồn như GitHub.