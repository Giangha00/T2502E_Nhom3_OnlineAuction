Triển khai tính năng chọn hàng loạt (**Check All**) và xóa các tài khoản đã chọn, chúng ta sẽ kết hợp giữa HTML Checkbox, JavaScript (jQuery) để xử lý giao diện và một Action trong Controller để xử lý xóa dữ liệu.

### Bước 1: Cập nhật View (Index.cshtml)

Chúng ta cần thêm một cột checkbox vào bảng và một nút "Xóa mục đã chọn".

```razor
@model YourProject.ViewModels.UserFilterViewModel

<div class="container mt-4">
    <h2 class="mb-4">Quản lý người dùng</h2>

    <!-- Form lọc giữ nguyên... -->

    <div class="mb-3">
        <!-- Nút xóa hàng loạt (mặc định ẩn hoặc disable) -->
        <button type="button" id="btnDeleteSelected" class="btn btn-danger" disabled>
            <i class="bi bi-trash"></i> Xóa các mục đã chọn (<span id="selectedCount">0</span>)
        </button>
    </div>

    <div class="table-responsive">
        <table class="table table-bordered table-hover">
            <thead class="table-dark">
                <tr>
                    <th style="width: 40px;">
                        <input type="checkbox" id="checkAll" class="form-check-input" />
                    </th>
                    <th>Họ tên</th>
                    <th>Email</th>
                    <th>Ngày tạo</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var user in Model.Users)
                {
                    <tr>
                        <td>
                            <input type="checkbox" class="user-checkbox form-check-input" value="@user.Id" />
                        </td>
                        <td>@user.FullName</td>
                        <td>@user.Email</td>
                        <td>@user.CreatedAt.ToString("dd/MM/yyyy HH:mm")</td>
                    </tr>
                }
            </tbody>
        </table>
    </div>

    <!-- Phân trang giữ nguyên... -->
</div>
```

---

### Bước 2: Viết JavaScript xử lý Check All và lấy IDs

Thêm đoạn script sau vào phần `@section Scripts` để xử lý logic chọn và gửi dữ liệu.

```javascript
@section Scripts {
    <script>
        $(function () {
            // 1. Xử lý Check All
            $('#checkAll').click(function () {
                $('.user-checkbox').prop('checked', this.checked);
                updateDeleteButton();
            });

            // 2. Xử lý khi nhấn từng checkbox lẻ
            $('.user-checkbox').click(function () {
                if (!this.checked) {
                    $('#checkAll').prop('checked', false);
                }
                if ($('.user-checkbox:checked').length === $('.user-checkbox').length) {
                    $('#checkAll').prop('checked', true);
                }
                updateDeleteButton();
            });

            // 3. Cập nhật trạng thái nút Xóa
            function updateDeleteButton() {
                var selectedIds = $('.user-checkbox:checked').length;
                $('#selectedCount').text(selectedIds);
                if (selectedIds > 0) {
                    $('#btnDeleteSelected').prop('disabled', false);
                } else {
                    $('#btnDeleteSelected').prop('disabled', true);
                }
            }

            // 4. Xử lý sự kiện nút Xóa (Sử dụng AJAX)
            $('#btnDeleteSelected').click(function () {
                var selectedIds = [];
                $('.user-checkbox:checked').each(function () {
                    selectedIds.push($(this).val());
                });

                if (confirm("Bạn có chắc chắn muốn xóa " + selectedIds.length + " tài khoản đã chọn?")) {
                    $.ajax({
                        url: '@Url.Action("DeleteSelected", "User")',
                        type: 'POST',
                        data: { ids: selectedIds },
                        success: function (response) {
                            if (response.success) {
                                alert(response.message);
                                location.reload(); // Tải lại trang để cập nhật danh sách
                            } else {
                                alert("Có lỗi xảy ra: " + response.message);
                            }
                        },
                        error: function () {
                            alert("Không thể kết nối đến máy chủ.");
                        }
                    });
                }
            });
        });
    </script>
}
```

---

### Bước 3: Cập nhật Controller xử lý xóa hàng loạt

Tại `UserController`, thêm một Action để nhận danh sách IDs và thực hiện xóa.

```csharp
[HttpPost]
[ValidateAntiForgeryToken] // Nên bổ sung để bảo mật (Cần gửi kèm Token nếu dùng AJAX phức tạp hơn)
public async Task<IActionResult> DeleteSelected(List<int> ids)
{
    if (ids == null || ids.Count == 0)
    {
        return Json(new { success = false, message = "Không có tài khoản nào được chọn." });
    }

    try
    {
        // Lấy danh sách các User có Id nằm trong list gửi lên
        var usersToDelete = await _context.Users.Where(u => ids.Contains(u.Id)).ToListAsync();
        
        if (usersToDelete.Any())
        {
            // Xóa cứng (Hard Delete)
            _context.Users.RemoveRange(usersToDelete);
            
            // Hoặc Xóa mềm (nếu bảng có trường Status)
            /*
            foreach(var user in usersToDelete) {
                user.Status = 0;
            }
            */
            
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"Đã xóa thành công {usersToDelete.Count} tài khoản." });
        }

        return Json(new { success = false, message = "Không tìm thấy dữ liệu phù hợp." });
    }
    catch (Exception ex)
    {
        return Json(new { success = false, message = "Lỗi: " + ex.Message });
    }
}
```

---

### Phân tích chuyên sâu:

1.  **Trải nghiệm người dùng (UX):** Nút xóa mặc định bị `disabled` và chỉ sáng lên khi người dùng chọn ít nhất một hàng. Điều này ngăn chặn việc nhấn nhầm.
2.  **Bảo mật:** Chúng ta sử dụng `HTTP POST` cho hành động xóa. Tuyệt đối không sử dụng `GET` để xóa dữ liệu vì rủi ro bảo mật (ví dụ: bị các bot tìm kiếm quét trúng link xóa).
3.  **Hiệu suất:** Lệnh `ids.Contains(u.Id)` trong LINQ sẽ được chuyển thành câu lệnh SQL `IN (1, 2, 3...)`. Đây là cách xóa hàng loạt tối ưu nhất trong EF Core.
4.  **Token bảo mật (Anti-Forgery Token):** Nếu bạn muốn dùng `[ValidateAntiForgeryToken]` với AJAX, bạn cần lấy token từ cookie/form và đính kèm vào Header của yêu cầu AJAX. Nếu ứng dụng nội bộ đơn giản, có thể tạm bỏ qua nhưng khuyến khích sử dụng cho sản phẩm thực tế.
5.  **Soft Delete:** Trong thực tế, các doanh nghiệp thường dùng xóa mềm (update trường `Status = 0`) thay vì xóa vĩnh viễn để có thể khôi phục dữ liệu khi cần hoặc phục vụ mục đích log/audit.