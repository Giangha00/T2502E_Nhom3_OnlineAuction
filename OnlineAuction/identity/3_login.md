# HƯỚNG DẪN XÂY DỰNG CHỨC NĂNG ĐĂNG NHẬP (LOGIN)

## 1. Khởi tạo LoginViewModel
Để đảm bảo tính an toàn và tách biệt dữ liệu, chúng ta tạo một ViewModel riêng cho trang đăng nhập thay vì dùng trực tiếp thực thể `AppUser`.

*Tập tin: `ViewModels/LoginViewModel.cs`*
```csharp
using System.ComponentModel.DataAnnotations;

namespace YourProject.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập Email")]
    [EmailAddress(ErrorMessage = "Định dạng Email không hợp lệ")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ghi nhớ đăng nhập?")]
    public bool RememberMe { get; set; }
    
    // Thuộc tính để lưu lại trang người dùng định truy cập trước khi bị yêu cầu đăng nhập
    public string? ReturnUrl { get; set; }
}
```

---

## 2. Xử lý Logic tại AccountController
Chúng ta sẽ sử dụng dịch vụ **`SignInManager<AppUser>`** để thực hiện quá trình kiểm tra thông tin và tạo Cookie định danh.

*Tập tin: `Controllers/AccountController.cs`*
```csharp
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using YourProject.Models;
using YourProject.ViewModels;

public class AccountController : Controller
{
    private readonly SignInManager<AppUser> _signInManager;
    private readonly UserManager<AppUser> _userManager;

    public AccountController(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager)
    {
        _signInManager = signInManager;
        _userManager = userManager;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        var model = new LoginViewModel { ReturnUrl = returnUrl };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        // Thực hiện đăng nhập
        // lockoutOnFailure: true sẽ khóa tài khoản nếu nhập sai nhiều lần (cấu hình trong Program.cs)
        var result = await _signInManager.PasswordSignInAsync(
            model.Email, 
            model.Password, 
            model.RememberMe, 
            lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // Kiểm tra và điều hướng về trang cũ hoặc trang chủ
            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        if (result.IsLockedOut)
        {
            ModelState.AddModelError("", "Tài khoản đã bị khóa do nhập sai nhiều lần. Vui lòng thử lại sau.");
        }
        else
        {
            ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác.");
        }

        return View(model);
    }
}
```

---

## 3. Xây dựng giao diện Đăng nhập (View)
Sử dụng các Tag Helpers của .NET để liên kết Form với ViewModel.

*Tập tin: `Views/Account/Login.cshtml`*
```razor
@model YourProject.ViewModels.LoginViewModel

<div class="container mt-5">
    <div class="row justify-content-center">
        <div class="col-md-4 shadow p-4 rounded bg-light">
            <h2 class="text-center mb-4">Đăng nhập</h2>
            
            <form asp-action="Login" method="post">
                <input type="hidden" asp-for="ReturnUrl" />
                
                <div asp-validation-summary="ModelOnly" class="text-danger mb-3"></div>

                <div class="mb-3">
                    <label asp-for="Email" class="form-label"></label>
                    <input asp-for="Email" class="form-control" />
                    <span asp-validation-for="Email" class="text-danger"></span>
                </div>

                <div class="mb-3">
                    <label asp-for="Password" class="form-label"></label>
                    <input asp-for="Password" class="form-control" />
                    <span asp-validation-for="Password" class="text-danger"></span>
                </div>

                <div class="mb-3 form-check">
                    <input asp-for="RememberMe" class="form-check-input" />
                    <label asp-for="RememberMe" class="form-check-label"></label>
                </div>

                <button type="submit" class="btn btn-primary w-100">Đăng nhập</button>
            </form>
        </div>
    </div>
</div>
```

---

## 4. Phân quyền và Bảo mật (Sử dụng [Authorize])

Sau khi đã đăng nhập thành công, bạn có thể bảo vệ các Controller hoặc Action khác bằng thuộc tính `[Authorize]`.

```csharp
[Authorize] // Yêu cầu phải đăng nhập mới được vào Controller này
public class DashboardController : Controller
{
    public IActionResult Index() => View();
}
```

---

## 5. Giải thích các kỹ thuật quan trọng

### 5.1. PasswordSignInAsync
Hàm này thực hiện nhiều tác vụ ngầm:
1.  Truy vấn người dùng dựa trên `UserName` (trong ví dụ này chúng ta gán UserName = Email khi đăng ký).
2.  So sánh mã băm (Hash) của mật khẩu nhập vào với mật khẩu trong DB.
3.  Nếu khớp, nó sẽ tạo ra một **ClaimsPrincipal** và ghi vào Cookie của trình duyệt.

### 5.2. ReturnUrl và IsLocalUrl
*   **ReturnUrl:** Khi người dùng cố truy cập `/Admin` mà chưa đăng nhập, Identity sẽ chuyển hướng đến `/Account/Login?ReturnUrl=/Admin`. Sau khi login xong, chúng ta dùng biến này để đưa họ quay lại đúng nơi họ cần.
*   **Url.IsLocalUrl:** Đây là bước kiểm tra bảo mật bắt buộc để ngăn chặn tấn công **Open Redirect** (kẻ xấu lừa người dùng đăng nhập rồi chuyển hướng họ sang một trang web độc hại bên ngoài).

### 5.3. LockoutOnFailure
Nếu được bật trong `Program.cs`, Identity sẽ đếm số lần đăng nhập sai. Khi đạt giới hạn (ví dụ 5 lần), tài khoản sẽ bị tạm khóa. Điều này giúp ngăn chặn các cuộc tấn công dò mật khẩu (Brute Force).

### 5.4. ValidateAntiForgeryToken
Luôn thêm thuộc tính này vào Action POST để bảo vệ ứng dụng khỏi tấn công **CSRF (Cross-Site Request Forgery)**.

Bây giờ hệ thống của bạn đã có luồng Đăng nhập hoàn chỉnh và an toàn. Bước tiếp theo thường là xử lý **Phân quyền (Roles)** để phân biệt người dùng thường và Admin.