# Speaker Notes — CardMarket Nhóm 3

## P01 — Tên đề tài
Chào hội đồng. Nhóm 3 xin trình bày đề tài CardMarket — RareCard Vault.

## P02 — Mục lục
17 trang: phần chung, 2 Use Case (User/Admin), 5 Activity, DB, kết luận.

## P03 — Giới thiệu thành viên
Nhóm trưởng: Nguyễn Giang Hà. Thành viên: Nguyễn Văn Hưng, Đinh Văn Hải, Phạm Việt Anh, Nguyễn Hữu Quân, Danil Famil Long.

## P04 — Tổng quan
Tổng quan hệ thống đấu giá & Buy Now.

## P05 — Mục tiêu đề tài
Bốn mục tiêu chính của đề tài.

## P06 — Công nghệ
Stack ASP.NET Core 8, Identity, PayPal, SignalR…

## P07 — Phân bố thời gian
Khung giai đoạn phát triển.

## P08 — UC User
Use Case Actor User: đăng ký, đăng nhập, khám phá, đăng bán, đấu giá, thông báo, đánh giá, thanh toán.

## P09 — UC Admin
Use Case Actor Admin: đăng nhập, quản lý user/danh mục/sản phẩm/phiên, khiếu nại, thống kê báo cáo.

## P10 — Act Auth
Activity dual cookie User / Admin.

## P11 — Act Đăng bán & Duyệt
Submit → confirming → Approve/Reject → live.

## P12 — Act Bid
Deposit → Place Bid → SignalR / Fraud.

## P13 — Act Checkout
Finalize → Order → PayPal/COD.

## P14 — Act Buy Now
Cart → Invoice 7 ngày → Thanh toán.

## P15 — Database
ER các entity chính.

## P16 — Kết luận
Điểm nổi bật dual auth, verify, realtime, i18n.

## P17 — Lời cảm ơn
Cảm ơn hội đồng, xin hỏi đáp.
