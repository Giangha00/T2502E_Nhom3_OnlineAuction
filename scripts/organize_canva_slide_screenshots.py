#!/usr/bin/env python3
"""Organize project screenshots/diagrams by Canva presentation slide structure."""

from __future__ import annotations

import json
import shutil
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path("/Users/macbook/RiderProjects/Nhom3")
OUT = ROOT / "_screenshots" / "canva_slides"
SHOTS = ROOT / "_screenshots"
TEAM = ROOT / "OnlineAuction" / "wwwroot" / "images" / "team"
DIAG_CHECK = ROOT / "_diagram_check"
DIAG_GEN = ROOT / "_diagram_gen"
CLASS_GEN = ROOT / "_class_gen"
SEQ = ROOT / "_seq_gen"


def ensure_dir(path: Path) -> Path:
    path.mkdir(parents=True, exist_ok=True)
    return path


def copy_as(src: Path, dest: Path) -> bool:
    if not src.exists():
        print(f"  MISSING {src}")
        return False
    dest.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(src, dest)
    print(f"  OK {dest.relative_to(OUT)}")
    return True


def first_glob(folder: Path, pattern: str) -> Path | None:
    matches = sorted(folder.glob(pattern))
    return matches[0] if matches else None


def find_font(size: int) -> ImageFont.FreeTypeFont | ImageFont.ImageFont:
    for path in (
        "/System/Library/Fonts/Supplemental/Arial.ttf",
        "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
        "/Library/Fonts/Arial.ttf",
    ):
        if Path(path).exists():
            return ImageFont.truetype(path, size)
    return ImageFont.load_default()


def make_team_collage(dest: Path) -> None:
    members = [
        ("pham-viet-anh.png", "Phạm Việt Anh", "Founder & CEO"),
        ("danil-fomin-long.png", "Danil Fomin Long", "Head of curation"),
        ("nguyen-van-hung.png", "Nguyễn Văn Hưng", "Technical director"),
        ("dinh-van-hai.png", "Đinh Văn Hải", "Marketplace operations"),
        ("nguyen-huu-quan.png", "Nguyễn Hữu Quân", "Trust & compliance"),
        ("nguyen-giang-ha.png", "Nguyễn Giang Hà", "Growth & community"),
    ]
    cell_w, cell_h = 320, 400
    cols, rows = 3, 2
    pad = 24
    img = Image.new("RGB", (cols * cell_w + (cols + 1) * pad, rows * cell_h + (rows + 1) * pad), (24, 24, 28))
    draw = ImageDraw.Draw(img)
    font_name = find_font(18)
    font_role = find_font(14)
    for i, (file, name, role) in enumerate(members):
        r, c = divmod(i, cols)
        x = pad + c * (cell_w + pad)
        y = pad + r * (cell_h + pad)
        avatar_path = TEAM / file
        if avatar_path.exists():
            av = Image.open(avatar_path).convert("RGB")
            av = av.resize((cell_w - 16, cell_w - 16))
            img.paste(av, (x + 8, y + 8))
        else:
            draw.rectangle([x + 8, y + 8, x + cell_w - 8, y + cell_w - 8], fill=(60, 60, 68))
        draw.text((x + 12, y + cell_w + 4), name, fill=(245, 245, 245), font=font_name)
        draw.text((x + 12, y + cell_w + 30), role, fill=(200, 160, 90), font=font_role)
        # also copy individual
        if avatar_path.exists():
            copy_as(avatar_path, dest.parent / f"avatar_{i+1:02d}_{file}")
    img.save(dest, quality=92)
    print(f"  GEN {dest.relative_to(OUT)}")


def make_architecture_diagram(dest: Path) -> None:
    w, h = 1400, 900
    img = Image.new("RGB", (w, h), (18, 20, 28))
    draw = ImageDraw.Draw(img)
    title_font = find_font(28)
    box_font = find_font(18)
    small_font = find_font(14)
    draw.text((40, 30), "CardMarket OnlineAuction — Architecture", fill=(240, 240, 245), font=title_font)

    layers = [
        (80, "Browser / Client", "Razor Views · Tailwind · jQuery · SignalR JS", (56, 120, 200)),
        (200, "ASP.NET Core MVC 8", "Controllers · Admin Area · Identity dual cookies", (70, 140, 110)),
        (320, "Service Layer", "Auction · Bid · Order · PayPal · Notification · Sell…", (120, 100, 180)),
        (440, "Data & Storage", "SQL Server (default) / MySQL · Cloudinary", (180, 120, 60)),
        (560, "Async & Integrations", "RabbitMQ · Firebase FCM · PayPal · Gmail API", (170, 80, 90)),
        (700, "Hosting & Workers", "Azure App Service · AuctionFinalizationWorker · RabbitMqConsumer", (80, 90, 120)),
    ]

    for y, title, detail, color in layers:
        draw.rounded_rectangle([80, y, w - 80, y + 90], radius=16, fill=color)
        draw.text((110, y + 18), title, fill=(255, 255, 255), font=box_font)
        draw.text((110, y + 50), detail, fill=(240, 240, 245), font=small_font)
        if y < 700:
            cx = w // 2
            draw.polygon([(cx - 12, y + 98), (cx + 12, y + 98), (cx, y + 112)], fill=(200, 200, 210))

    draw.text((80, h - 40), "Source: Program.cs · OnlineAuction.csproj · README.md", fill=(160, 160, 170), font=small_font)
    img.save(dest, quality=92)
    print(f"  GEN {dest.relative_to(OUT)}")


def make_toc_banner(dest: Path) -> None:
    items = [
        "01 Bài toán",
        "02 Chức năng User",
        "03 Chức năng Admin",
        "04 Activity",
        "05 Kiến trúc",
        "06 Database",
        "07 Công nghệ",
        "08 Kết luận",
    ]
    w, h = 1400, 420
    img = Image.new("RGB", (w, h), (16, 18, 24))
    draw = ImageDraw.Draw(img)
    draw.text((48, 36), "Mục lục trình bày", fill=(245, 245, 248), font=find_font(32))
    box_w = 300
    box_h = 70
    gap = 20
    start_y = 110
    for i, label in enumerate(items):
        r, c = divmod(i, 4)
        x = 48 + c * (box_w + gap)
        y = start_y + r * (box_h + gap)
        draw.rounded_rectangle([x, y, x + box_w, y + box_h], radius=12, fill=(36, 40, 52), outline=(90, 90, 110))
        draw.text((x + 20, y + 22), label, fill=(230, 230, 235), font=find_font(18))
    img.save(dest, quality=92)
    print(f"  GEN {dest.relative_to(OUT)}")


def make_tech_banner(dest: Path) -> None:
    techs = [
        "ASP.NET Core 8",
        "EF Core / SQL Server",
        "Tailwind + Razor",
        "Identity",
        "Cloudinary",
        "RabbitMQ",
        "Firebase FCM",
        "PayPal",
        "Azure",
        "SignalR",
        "xUnit / Playwright",
    ]
    w, h = 1400, 520
    img = Image.new("RGB", (w, h), (12, 14, 20))
    draw = ImageDraw.Draw(img)
    draw.text((48, 36), "Tech Stack — CardMarket OnlineAuction", fill=(245, 245, 248), font=find_font(28))
    colors = [
        (0, 120, 215),
        (200, 80, 60),
        (20, 160, 140),
        (90, 90, 200),
        (180, 100, 40),
        (180, 50, 50),
        (240, 160, 40),
        (0, 100, 180),
        (40, 100, 180),
        (70, 130, 180),
        (90, 120, 90),
    ]
    for i, name in enumerate(techs):
        r, c = divmod(i, 4)
        x = 48 + c * 330
        y = 110 + r * 110
        draw.rounded_rectangle([x, y, x + 300, y + 80], radius=14, fill=colors[i % len(colors)])
        draw.text((x + 24, y + 28), name, fill=(255, 255, 255), font=find_font(20))
    img.save(dest, quality=92)
    print(f"  GEN {dest.relative_to(OUT)}")


def make_conclusion_banner(dest: Path) -> None:
    cols = [
        ("Achieved", "Auction + Buy Now\nAdmin verify\nPayPal + realtime", (40, 120, 80)),
        ("Hard", "Dual session\nOrder sync\nFraud / anti-snipe", (140, 90, 40)),
        ("Strength", "Service layer\nPermissions\nSignalR + FCM", (50, 90, 150)),
        ("Next", "Google OAuth\nSeller rating\nPayPal live", (90, 70, 140)),
    ]
    w, h = 1400, 480
    img = Image.new("RGB", (w, h), (16, 18, 24))
    draw = ImageDraw.Draw(img)
    draw.text((48, 30), "Kết luận — roadmap", fill=(245, 245, 248), font=find_font(28))
    box_w = 300
    for i, (title, body, color) in enumerate(cols):
        x = 48 + i * (box_w + 24)
        y = 100
        draw.rounded_rectangle([x, y, x + box_w, y + 320], radius=16, fill=color)
        draw.text((x + 20, y + 24), title, fill=(255, 255, 255), font=find_font(24))
        draw.multiline_text((x + 20, y + 90), body, fill=(240, 240, 245), font=find_font(18), spacing=12)
    img.save(dest, quality=92)
    print(f"  GEN {dest.relative_to(OUT)}")


def make_split_collage(left: Path, right: Path, dest: Path, labels=("Marketplace", "Auction Detail")) -> None:
    if not left.exists() or not right.exists():
        print(f"  SKIP collage missing {left.name if not left.exists() else right.name}")
        return
    a = Image.open(left).convert("RGB")
    b = Image.open(right).convert("RGB")
    target_h = 800
    a = a.resize((int(a.width * target_h / a.height), target_h))
    b = b.resize((int(b.width * target_h / b.height), target_h))
    gap = 16
    img = Image.new("RGB", (a.width + b.width + gap + 40, target_h + 80), (20, 22, 28))
    img.paste(a, (20, 50))
    img.paste(b, (20 + a.width + gap, 50))
    draw = ImageDraw.Draw(img)
    draw.text((20, 12), labels[0], fill=(230, 230, 235), font=find_font(18))
    draw.text((20 + a.width + gap, 12), labels[1], fill=(230, 230, 235), font=find_font(18))
    img.save(dest, quality=90)
    print(f"  GEN {dest.relative_to(OUT)}")


def make_triple_collage(paths: list[Path], dest: Path, labels: list[str]) -> None:
    images = []
    for p in paths:
        if not p.exists():
            print(f"  SKIP triple missing {p}")
            return
        images.append(Image.open(p).convert("RGB"))
    target_h = 700
    resized = [im.resize((int(im.width * target_h / im.height), target_h)) for im in images]
    gap = 12
    total_w = sum(im.width for im in resized) + gap * (len(resized) - 1) + 40
    img = Image.new("RGB", (total_w, target_h + 70), (20, 22, 28))
    draw = ImageDraw.Draw(img)
    x = 20
    for im, label in zip(resized, labels):
        draw.text((x, 12), label, fill=(230, 230, 235), font=find_font(16))
        img.paste(im, (x, 45))
        x += im.width + gap
    img.save(dest, quality=88)
    print(f"  GEN {dest.relative_to(OUT)}")


def main():
    if OUT.exists():
        shutil.rmtree(OUT)
    ensure_dir(OUT)

    manifest = []

    # ---- Slide 1 ----
    print("\n# Slide 1 — Giới thiệu đề tài")
    d = ensure_dir(OUT / "01_gioi_thieu_de_tai")
    copy_as(SHOTS / "01_home.png", d / "01_home_hero.png")
    manifest.append({"slide": 1, "title": "Giới thiệu đề tài", "folder": str(d.relative_to(ROOT)), "primary": "01_home_hero.png"})

    # ---- Slide 2 ----
    print("\n# Slide 2 — Thành viên")
    d = ensure_dir(OUT / "02_thanh_vien")
    make_team_collage(d / "02_team_collage.png")
    manifest.append({"slide": 2, "title": "Giới thiệu thành viên", "folder": str(d.relative_to(ROOT)), "primary": "02_team_collage.png"})

    # ---- Slide 3 ----
    print("\n# Slide 3 — Mục lục")
    d = ensure_dir(OUT / "03_muc_luc")
    make_toc_banner(d / "03_toc_banner.png")
    manifest.append({"slide": 3, "title": "Mục lục", "folder": str(d.relative_to(ROOT)), "primary": "03_toc_banner.png"})

    # ---- Slide 4 ----
    print("\n# Slide 4 — Bài toán")
    d = ensure_dir(OUT / "04_gioi_thieu_bai_toan")
    left = SHOTS / "04_auction_marketplace.png"
    if not left.exists():
        left = SHOTS / "02_auction_list.png"
    right = SHOTS / "05_auction_detail.png"
    if not right.exists():
        right = SHOTS / "04_auction_detail.png"
    copy_as(left, d / "04a_auction_marketplace.png")
    copy_as(right, d / "04b_auction_detail.png")
    make_split_collage(left, right, d / "04_split_marketplace_detail.png")
    manifest.append({"slide": 4, "title": "Giới thiệu bài toán", "folder": str(d.relative_to(ROOT)), "primary": "04_split_marketplace_detail.png"})

    # ---- Slide 5 ----
    print("\n# Slide 5 — Chức năng User")
    d = ensure_dir(OUT / "05_chuc_nang_nguoi_dung")
    login = SHOTS / "05_login.png"
    if not login.exists():
        login = SHOTS / "02_login_modal.png"
    bid = SHOTS / "14_place_bid_view.png"
    if not bid.exists():
        bid = SHOTS / "15_auction_detail_bid_area.png"
    orders = SHOTS / "11_orders.png"
    if not orders.exists():
        orders = SHOTS / "13_order_center.png"
    extras = [
        (login, "05a_login.png"),
        (bid, "05b_place_bid.png"),
        (orders, "05c_orders.png"),
        (SHOTS / "06_signup.png", "05d_signup.png"),
        (SHOTS / "02_auction_list.png", "05e_auction_list.png"),
        (SHOTS / "03_buynow_list.png", "05f_buynow_list.png"),
        (SHOTS / "08_my_bids.png", "05g_my_bids.png"),
        (SHOTS / "09_watchlist.png", "05h_watchlist.png"),
        (SHOTS / "12_create_auction.png", "05i_create_auction.png"),
    ]
    for src, name in extras:
        copy_as(src, d / name)
    make_triple_collage([login, bid, orders], d / "05_collage_auth_bid_order.png", ["Login", "Place Bid", "Orders"])
    manifest.append({"slide": 5, "title": "Chức năng người dùng", "folder": str(d.relative_to(ROOT)), "primary": "05_collage_auth_bid_order.png"})

    # ---- Slide 6 ----
    print("\n# Slide 6 — Admin")
    d = ensure_dir(OUT / "06_chuc_nang_admin")
    admin_files = [
        ("16_admin_dashboard.png", "06a_dashboard.png"),
        ("15_admin_login.png", "06b_admin_login.png"),
        ("17_admin_users.png", "06c_users.png"),
        ("18_admin_auctions.png", "06d_auctions.png"),
        ("19_admin_verify.png", "06e_verify.png"),
        ("20_admin_category.png", "06f_category.png"),
        ("21_admin_product.png", "06g_product.png"),
        ("22_admin_buynow.png", "06h_buynow.png"),
        ("23_admin_complaints.png", "06i_complaints.png"),
    ]
    for src_name, dest_name in admin_files:
        copy_as(SHOTS / src_name, d / dest_name)
    make_triple_collage(
        [SHOTS / "16_admin_dashboard.png", SHOTS / "19_admin_verify.png", SHOTS / "23_admin_complaints.png"],
        d / "06_collage_dashboard_verify_complaints.png",
        ["Dashboard", "Verify", "Complaints"],
    )
    manifest.append({"slide": 6, "title": "Chức năng Admin", "folder": str(d.relative_to(ROOT)), "primary": "06_collage_dashboard_verify_complaints.png"})

    # ---- Slide 7 ----
    print("\n# Slide 7 — Activity Diagram")
    d = ensure_dir(OUT / "07_activity_diagram")
    act_map = [
        ("ACT_4_2_1_Register_Account_Flow*.png", "07a_register_account.png"),
        ("ACT_4_2_2_Login_Flow*.png", "07b_login.png"),
        ("ACT_4_2_4_Place_Bid_Flow*.png", "07c_place_bid.png"),
        ("ACT_4_2_5_Auction_Closing_Flow*.png", "07d_auction_closing.png"),
        ("ACT_4_2_6_Payment_Flow*.png", "07e_payment.png"),
    ]
    for pattern, dest_name in act_map:
        src = first_glob(DIAG_CHECK, pattern)
        if src:
            copy_as(src, d / dest_name)
    copy_as(DIAG_GEN / "act_register_auction.png", d / "07f_act_register_auction.png")
    copy_as(DIAG_GEN / "act_browse.png", d / "07g_act_browse.png")
    # primary flow collage from key activities
    primary_candidates = [
        d / "07a_register_account.png",
        d / "07c_place_bid.png",
        d / "07e_payment.png",
    ]
    if all(p.exists() for p in primary_candidates):
        make_triple_collage(primary_candidates, d / "07_collage_register_bid_payment.png", ["Register", "Bid", "Payment"])
    # usecase samples
    for i, uc in enumerate(sorted(DIAG_CHECK.glob("UC_4_3_*.png"))[:4], start=1):
        copy_as(uc, d / f"07uc_{i:02d}_{uc.stem[:40]}.png")
    # sequence samples matching flow
    for name, dest_name in [
        ("Register_Account.png", "07seq_register.png"),
        ("Login.png", "07seq_login.png"),
        ("Place_Bid.png", "07seq_place_bid.png"),
        ("Payment.png", "07seq_payment.png"),
        ("Browse_Marketplace.png", "07seq_browse.png"),
    ]:
        copy_as(SEQ / name, d / dest_name)
    manifest.append({"slide": 7, "title": "Activity Diagram", "folder": str(d.relative_to(ROOT)), "primary": "07_collage_register_bid_payment.png"})

    # ---- Slide 8 ----
    print("\n# Slide 8 — Kiến trúc")
    d = ensure_dir(OUT / "08_kien_truc_he_thong")
    make_architecture_diagram(d / "08_architecture_diagram.png")
    manifest.append({"slide": 8, "title": "Kiến trúc hệ thống", "folder": str(d.relative_to(ROOT)), "primary": "08_architecture_diagram.png"})

    # ---- Slide 9 ----
    print("\n# Slide 9 — Database")
    d = ensure_dir(OUT / "09_database")
    copy_as(CLASS_GEN / "class_diagram.png", d / "09_class_erd_diagram.png")
    manifest.append({"slide": 9, "title": "Database", "folder": str(d.relative_to(ROOT)), "primary": "09_class_erd_diagram.png"})

    # ---- Slide 10 ----
    print("\n# Slide 10 — Công nghệ")
    d = ensure_dir(OUT / "10_cong_nghe")
    make_tech_banner(d / "10_tech_stack_banner.png")
    # supporting UI proof screenshots
    copy_as(SHOTS / "01_home.png", d / "10a_frontend_home.png")
    copy_as(SHOTS / "16_admin_dashboard.png", d / "10b_admin_backend_ui.png")
    manifest.append({"slide": 10, "title": "Công nghệ sử dụng", "folder": str(d.relative_to(ROOT)), "primary": "10_tech_stack_banner.png"})

    # ---- Slide 11 ----
    print("\n# Slide 11 — Kết luận")
    d = ensure_dir(OUT / "11_ket_luan")
    make_conclusion_banner(d / "11_conclusion_roadmap.png")
    copy_as(SHOTS / "07_after_login_home.png", d / "11a_product_home_logged_in.png")
    copy_as(SHOTS / "16_admin_dashboard.png", d / "11b_admin_dashboard.png")
    manifest.append({"slide": 11, "title": "Kết luận", "folder": str(d.relative_to(ROOT)), "primary": "11_conclusion_roadmap.png"})

    # ---- Slide 12 ----
    print("\n# Slide 12 — Cảm ơn")
    d = ensure_dir(OUT / "12_cam_on")
    copy_as(SHOTS / "01_home.png", d / "12_home_hero_thanks.png")
    manifest.append({"slide": 12, "title": "Cảm ơn", "folder": str(d.relative_to(ROOT)), "primary": "12_home_hero_thanks.png"})

    # README
    readme = OUT / "README.md"
    lines = [
        "# Screenshots theo cấu trúc slide Canva",
        "",
        "Nguồn yêu cầu: `docs/CardMarket_OnlineAuction_Canva_Presentation.pdf`",
        "",
        "| Slide | Tiêu đề | Thư mục | Ảnh chính (đưa vào Canva) |",
        "|------:|---------|---------|---------------------------|",
    ]
    for item in manifest:
        lines.append(
            f"| {item['slide']} | {item['title']} | `{item['folder']}` | `{item.get('primary','')}` |"
        )
    lines += [
        "",
        "## Gợi ý dùng Canva",
        "- Mỗi thư mục `01_…` → `12_…` tương ứng 1 slide.",
        "- Ưu tiên file `*_collage_*`, `*_banner.png`, `*_diagram.png` làm ảnh chính.",
        "- Các file còn lại trong cùng folder dùng làm ảnh phụ / zoom.",
        "",
        "## Mapping từ PDF Image Suggestion",
        "- Slide 1: `_screenshots/01_home.png`",
        "- Slide 2: `OnlineAuction/wwwroot/images/team/*.png` → collage",
        "- Slide 4: marketplace + detail",
        "- Slide 5: login + place bid + orders",
        "- Slide 6: admin dashboard → verify → complaints",
        "- Slide 7: ACT diagrams + sequence",
        "- Slide 8: architecture diagram (generated — repo không có PNG sẵn)",
        "- Slide 9: `_class_gen/class_diagram.png`",
        "- Slide 12: homepage hero",
        "",
    ]
    readme.write_text("\n".join(lines), encoding="utf-8")
    (OUT / "manifest.json").write_text(json.dumps(manifest, indent=2, ensure_ascii=False), encoding="utf-8")

    # count
    pngs = list(OUT.rglob("*.png"))
    print(f"\nDONE: {len(pngs)} images in {OUT}")
    print(f"Index: {readme}")


if __name__ == "__main__":
    main()
