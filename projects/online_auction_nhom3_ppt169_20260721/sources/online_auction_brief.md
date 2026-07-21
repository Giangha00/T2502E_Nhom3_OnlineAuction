# CardMarket (RareCard Vault) — Nền tảng đấu giá thẻ bài trực tuyến

## Thông tin dự án
- **Tên đề tài / sản phẩm:** CardMarket — RareCard Vault (OnlineAuction)
- **Nhóm:** Nhóm 3 (Nhom3)
- **Loại:** Đồ án / bài thuyết trình môn học — hệ thống web đấu giá online chuyên biệt thẻ bài sưu tầm (TCG, sports cards, grading PSA…)
- **Solution:** `Nhom3.sln` · App: ASP.NET Core 8 MVC

## Thành viên nhóm (từ lịch sử git — cần xác nhận tên đầy đủ & vai trò)
| # | Tên / identity | Ghi chú |
|---|----------------|---------|
| 1 | Giang Hà (Giangha00 / Ha) | Thành viên chính (nhiều commit) |
| 2 | Nguyễn Hưng (Nguyen-Hung03) | Thành viên |
| 3 | Phạm Việt Anh (Vanhpham-human) | Thành viên |
| 4 | Nguyễn Hữu Quân | Thành viên |
| 5 | azhai4123 | Cần xác nhận tên đầy đủ |
| — | Daniil-L54 | Collaborator ngoài (tùy chọn đưa vào slide) |

> Trang About trên UI dùng nhân vật giả (Julian Thorne…) — KHÔNG dùng làm thành viên nhóm.

## Cấu trúc slide bắt buộc (do người dùng chỉ định — giữ đúng thứ tự)
1. Tên đề tài
2. Mục lục
3. Giới thiệu thành viên
4. Tổng quan
5. Mục tiêu đề tài
6. Công nghệ
7. Phân bố thời gian
8. Use Case
9. Activity Diagram
10. Db diagram
11. Kết luận
12. Lời cảm ơn

## Tổng quan hệ thống
Nền tảng **đấu giá & mua ngay (Buy Now)** thẻ bài sưu tầm:
- Người bán đăng listing → **Admin duyệt** → công khai
- Người mua **đăng ký + đặt cọc** → **đặt giá (bid)** realtime
- Kết thúc phiên → tạo đơn → **thanh toán PayPal / COD**
- Hỗ trợ: watchlist, thông báo realtime/push, khiếu nại/hoàn tiền, đa ngôn ngữ (vi/en/ja/ko), khu vực Admin

## Mục tiêu đề tài
1. Xây dựng hệ thống đấu giá online hoàn chỉnh (MVC + Identity + DB)
2. Mô phỏng quy trình thương mại thực tế: xác thực listing, cọc đăng ký, chống gian lận bid, thanh toán, phí nền tảng
3. Tách bảo mật Admin/User (dual session cookies), phân quyền động
4. Trải nghiệm realtime + đa ngôn ngữ cho người dùng

## Công nghệ
| Nhóm | Stack |
|------|--------|
| Backend | ASP.NET Core 8 MVC, EF Core 9, Identity |
| Database | MySQL (Pomelo); hỗ trợ SQL Server / SQLite |
| Auth | Dual cookie: User (`.AuctionHouse.User`) / Admin (`.AuctionHouse.Admin`) |
| Thanh toán | PayPal Sandbox (REST + IPN/Webhook), COD |
| Realtime | SignalR (`/hubs/app`) |
| Messaging | RabbitMQ |
| Cloud/Media | Cloudinary (ảnh), Firebase FCM (push), Azure App Service |
| Frontend | Razor Views, Tailwind CSS 4, jQuery validation |
| Localization | en-US, vi-VN, ja-JP, ko-KR |
| Khác | ClosedXML, Bogus (seed data) |

## Phân bố thời gian
Không có Gantt/timeline chính thức trong repository. Gợi ý khung thuyết trình (scenario data — cần nhóm xác nhận):
| Giai đoạn | Nội dung | Tỷ lệ gợi ý |
|-----------|----------|-------------|
| Giai đoạn 1 | Phân tích yêu cầu, Use Case, DB design | ~15% |
| Giai đoạn 2 | Auth, Identity dual-session, Admin | ~20% |
| Giai đoạn 3 | Auction listing, verify, Sell flow | ~20% |
| Giai đoạn 4 | Bid realtime, đăng ký/cọc, anti-fraud | ~20% |
| Giai đoạn 5 | Order/PayPal, Buy Now, thông báo | ~15% |
| Giai đoạn 6 | Đa ngôn ngữ, polish, smoke test, demo | ~10% |

## Use Case — Actors
- **Guest:** xem Home/Auction/BuyNow/FAQ/About/Contact; đổi ngôn ngữ
- **User / Buyer:** đăng ký/đăng nhập; đăng ký đấu giá + cọc; bid; Buy Now; watchlist; Payment Center; hoàn tiền; thông báo
- **Seller** (role User): `/Sell` tạo Auction/Buy Now; theo dõi submissions
- **Admin:** login riêng `/Admin`; duyệt listing; CRUD auction/product/category/user; permission; complaint; dashboard

## Activity flows chính
1. **Auth:** SignUp → Confirm email → Login User / Login Admin (cookie độc lập)
2. **Bán & duyệt:** Seller submit → `confirming` → Admin Approve/Reject → `scheduled`/`live`
3. **Đăng ký & bid:** Register → Deposit PayPal → PlaceBid (+ anti-snipe/fraud) → SignalR cập nhật
4. **Kết thúc & checkout:** Finalization worker → Order `auction_win` → `/Order` → PayPal/COD
5. **Buy Now:** AddToCart → invoice → thanh toán (deadline 7 ngày)

## Database — entities chính
`ApplicationUser`, `Category`, `Product` (+ images/documents/templates), `Auction`, `Bid`, `BidFraudAlert`, `AuctionRegistration` (+ deposit), `AuctionOrder` (+ OrderItem, Payment), `WatchlistItem`, `Notification`, `UserDeviceToken`, `Complaint`, `Permission` / `RolePermission` / `UserPermission`, `UserOtpCode`, `UserSandboxWallet`, `WinnerNonPaymentLog`

### Quan hệ then chốt
- User 1–N Product (Seller)
- Product 1–N Auction
- Auction 1–N Bid / Registration / Watchlist
- Auction N–1 Winner (User)
- Order N–1 Buyer; Order 1–N OrderItem → Auction
- Permission gắn Role/User (Admin động)

## Điểm nổi bật (cho Kết luận)
- Dual-session auth (User ↔ Admin độc lập)
- Verify listing trước khi public
- Đấu giá có cọc đăng ký + chống gian lận bid
- SignalR realtime + Firebase push + RabbitMQ
- Đa ngôn ngữ 4 locale; Cloudinary; PayPal end-to-end
- Domain chuyên biệt: thẻ bài đã grading / authenticity

## Lời cảm ơn
Cảm ơn giảng viên hướng dẫn, các thành viên nhóm, và những người đã hỗ trợ trong quá trình phát triển dự án.
