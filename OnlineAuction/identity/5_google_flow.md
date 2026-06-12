# **Xác thực ủy quyền (Delegated Authentication)**.

Dưới đây là 3 bước đơn giản để hiểu bản chất:

### 1. Phân vai các nhân vật
*   **Người dùng (User):** Khách hàng muốn vào cửa hàng của bạn.
*   **Hệ thống của bạn (Client/Your App):** Cửa hàng yêu cầu phải có thẻ thành viên mới cho vào.
*   **Google (Identity Provider):** Cơ quan cấp hộ chiếu cực kỳ uy tín mà ai cũng có tài khoản.

---

### 2. Quy trình "Bắt tay" (The Handshake)

Quy trình này diễn ra theo 5 bước logic sau:

1.  **Yêu cầu xác nhận danh tính:** Người dùng nhấn nút "Login with Google". Hệ thống của bạn nói: *"Tôi không biết bạn là ai, hãy sang ông Google lấy một mảnh giấy xác nhận danh tính mang về đây cho tôi."*
2.  **Người dùng cấp quyền:** Người dùng sang trang của Google. Google hỏi: *"Bạn có đồng ý cho cửa hàng 'Hệ thống của bạn' xem tên và Email của bạn không?"*. Người dùng chọn **"Đồng ý"**.
3.  **Trao "Giấy thông hành":** Google đưa cho Người dùng một "Mảnh giấy xác nhận" (được gọi là **Authorization Code**) và bảo: *"Cầm cái này về đưa cho cửa hàng kia."*
4.  **Kiểm tra chéo (Cực kỳ quan trọng):** Hệ thống của bạn nhận được mảnh giấy đó, nhưng để chắc chắn không phải đồ giả, hệ thống của bạn sẽ **bí mật gọi điện cho Google** (gọi API) hỏi: *"Ông vừa đưa mảnh giấy mã số XYZ này cho ai đó phải không? Có đúng đó là Nguyễn Văn A không?"*
5.  **Cấp thẻ thành viên:** Google xác nhận: *"Đúng, đó là Nguyễn Văn A, Email là a@gmail.com đấy"*. Lúc này, hệ thống của bạn tin tưởng tuyệt đối, tạo một cái "Thẻ thành viên" (Cookie đăng nhập) và cho phép người dùng vào hệ thống.

---

### 3. Bản chất kỹ thuật (Cái hay của nó nằm ở đây)

Tại sao cách này lại an toàn và được ưa chuộng?

*   **Bạn không bao giờ biết mật khẩu:** Hệ thống của bạn chỉ nhận được thông tin (Tên, Email, Ảnh) từ Google trả về. Bạn không hề biết mật khẩu Google của khách là gì. Nếu hệ thống của bạn bị hack, mật khẩu của khách vẫn an toàn ở Google.
*   **Sự tin tưởng (Trust):** Bạn tin tưởng Google vì bạn và Google đã trao đổi một "Mật mã bí mật" (Client Secret) từ trước. Chỉ có bạn và Google biết mã này để xác nhận thông tin của nhau.
*   **Liên kết tài khoản (Account Mapping):** Trong database Identity của bạn, bạn sẽ có một bảng tên là `AspNetUserLogins`. Nó đóng vai trò ghi chú: *"Người dùng tên A trong hệ thống của tôi thực chất chính là ông có mã ID 123456 bên Google"*. Lần sau họ quay lại, bạn chỉ cần tra bảng này là xong.

### Tóm lại:
Đăng nhập bằng Google là việc **"Mượn sự tin tưởng"**. Google đứng ra bảo lãnh: *"Tôi thề đây là Nguyễn Văn A, tôi đã kiểm tra mật khẩu của anh ta rồi, ông cứ cho anh ta vào đi!"*. Hệ thống của bạn chỉ việc mở cửa.