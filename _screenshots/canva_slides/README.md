# Screenshots theo cấu trúc slide Canva

Nguồn yêu cầu: `docs/CardMarket_OnlineAuction_Canva_Presentation.pdf`

| Slide | Tiêu đề | Thư mục | Ảnh chính (đưa vào Canva) |
|------:|---------|---------|---------------------------|
| 1 | Giới thiệu đề tài | `_screenshots/canva_slides/01_gioi_thieu_de_tai` | `01_home_hero.png` |
| 2 | Giới thiệu thành viên | `_screenshots/canva_slides/02_thanh_vien` | `02_team_collage.png` |
| 3 | Mục lục | `_screenshots/canva_slides/03_muc_luc` | `03_toc_banner.png` |
| 4 | Giới thiệu bài toán | `_screenshots/canva_slides/04_gioi_thieu_bai_toan` | `04_split_marketplace_detail.png` |
| 5 | Chức năng người dùng | `_screenshots/canva_slides/05_chuc_nang_nguoi_dung` | `05_collage_auth_bid_order.png` |
| 6 | Chức năng Admin | `_screenshots/canva_slides/06_chuc_nang_admin` | `06_collage_dashboard_verify_complaints.png` |
| 7 | Activity Diagram | `_screenshots/canva_slides/07_activity_diagram` | `07_collage_register_bid_payment.png` |
| 8 | Kiến trúc hệ thống | `_screenshots/canva_slides/08_kien_truc_he_thong` | `08_architecture_diagram.png` |
| 9 | Database | `_screenshots/canva_slides/09_database` | `09_class_erd_diagram.png` |
| 10 | Công nghệ sử dụng | `_screenshots/canva_slides/10_cong_nghe` | `10_tech_stack_banner.png` |
| 11 | Kết luận | `_screenshots/canva_slides/11_ket_luan` | `11_conclusion_roadmap.png` |
| 12 | Cảm ơn | `_screenshots/canva_slides/12_cam_on` | `12_home_hero_thanks.png` |

## Gợi ý dùng Canva
- Mỗi thư mục `01_…` → `12_…` tương ứng 1 slide.
- Ưu tiên file `*_collage_*`, `*_banner.png`, `*_diagram.png` làm ảnh chính.
- Các file còn lại trong cùng folder dùng làm ảnh phụ / zoom.

## Mapping từ PDF Image Suggestion
- Slide 1: `_screenshots/01_home.png`
- Slide 2: `OnlineAuction/wwwroot/images/team/*.png` → collage + `02_about_us_page.png` (/AboutUs/About)
- Slide 4: marketplace + detail
- Slide 5: login + place bid + orders
- Slide 6: admin dashboard → verify → complaints
- Slide 7: ACT diagrams + sequence
- Slide 8: architecture diagram (generated — repo không có PNG sẵn)
- Slide 9: `_class_gen/class_diagram.png`
- Slide 12: homepage hero
