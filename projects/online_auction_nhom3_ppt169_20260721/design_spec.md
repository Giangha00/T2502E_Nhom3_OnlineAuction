# CardMarket (RareCard Vault) - Design Spec

<!-- ppt-master-schema: design-spec/v1 -->

## I. Project Information

| Item | Value |
| --- | --- |
| Project Name | online_auction_nhom3 |
| Canvas Format | PPT 16:9 (1280×720) |
| Page Count | 20 |
| Target Audience | Giảng viên và hội đồng đánh giá đồ án |
| Communication Intent | Báo cáo đề tài CardMarket: nhóm, tổng quan, mục tiêu, công nghệ, timeline, Use Case, Activity, DB |
| Desired Audience Outcome | Hiểu sản phẩm, kiến trúc và quy trình nghiệp vụ |
| Core Message / Ask / Action | CardMarket mô phỏng đầy đủ vòng đời đấu giá thẻ bài trực tuyến trên ASP.NET Core 8 |
| Delivery Context | Thuyết trình 10–15 phút |
| Artifact Afterlife | Nộp bài / lưu trữ đồ án |
| Reading Mode | balanced |
| Content Strategy | Giữ đúng 12 mục cấu trúc người dùng chỉ định |
| Design Style | soft-rounded, RareCard blue/slate (site.css) |
| Formula Policy | text-only |
| AI Image Acquisition Path | not applicable |
| Generation Mode | continuous |
| Spec Refinement | disabled |
| Created Date | 2026-07-21 |

## II. Canvas Specification

| Property | Value |
| --- | --- |
| Format | PPT 16:9 |
| Dimensions | 1280×720 |
| viewBox | `0 0 1280 720` |
| Margins | 60px |
| Content Area | 1160×600 |

## III. Visual Theme

### Theme Style

- **Mode**: pyramid
- **Visual style**: soft-rounded
- **Theme**: CardMarket professional
- **Tone**: Warm, trustworthy, student project defense

### Color Scheme

| Role | HEX | Purpose |
| --- | --- | --- |
| Background | #F8FAFC | Page background |
| Secondary background | #DBEAFE | Cards |
| Primary | #1D4ED8 | Headings, accents |
| Accent | #0F172A | Strong emphasis |
| Secondary accent | #64748B | Labels |
| Body text | #334155 | Body copy |

## IV. Typography System

### Font Plan

| Role | Chinese | English | Fallback tail |
| --- | --- | --- | --- |
| Title | Arial | Arial | sans-serif |
| Body | Arial | Arial | sans-serif |

- **Title stack**: Arial, sans-serif
- **Body stack**: Arial, sans-serif
- **Role rationale**: Clean sans-serif for Vietnamese academic presentation

### Font Size Hierarchy

| Purpose | Anchor Size (px) |
| --- | ---: |
| Body | 22 |
| Title | 36 |
| Subtitle | 28 |
| Annotation | 18 |

## V. Layout Principles

### Page Structure

- **Header area**: Title bar with primary accent line
- **Content area**: Rounded cards and diagrams
- **Footer area**: Nhóm 3 · CardMarket + page number

## VI. Icon System

- **Library**: tabler-outline
- **Style**: Outline, primary color

## VII. Data Visualization

No native charts.

## VIII. Image Resource List

No images (diagrams drawn in SVG).

## IX. Content Outline

### P01 — Tên đề tài
- **Title**: CardMarket (RareCard Vault)
- **Core message**: Nền tảng đấu giá thẻ bài trực tuyến
- **Audience move**: Establish project identity

### P02 — Mục lục
- **Title**: Mục lục
- **Content**: 12 sections list
- **Audience move**: Orient the jury

### P03 — Giới thiệu thành viên
- **Title**: Giới thiệu thành viên
- **Content**: 5 team members
- **Audience move**: Introduce team

### P04 — Tổng quan
- **Title**: Tổng quan hệ thống
- **Audience move**: Explain what the system does

### P05 — Mục tiêu đề tài
- **Title**: Mục tiêu đề tài
- **Audience move**: State goals

### P06 — Công nghệ
- **Title**: Công nghệ sử dụng
- **Audience move**: Show technical stack

### P07 — Phân bố thời gian
- **Title**: Phân bố thời gian
- **Audience move**: Present timeline (scenario)

### P08 — Use Case
- **Title**: Use Case
- **Audience move**: Show actors and use cases

### P09 — Activity Diagram
- **Title**: Activity Diagram
- **Audience move**: Walk through auction flow

### P10 — Db diagram
- **Title**: Database Diagram
- **Audience move**: Show data model

### P11 — Kết luận
- **Title**: Kết luận
- **Audience move**: Summarize achievements

### P12 — Lời cảm ơn
- **Title**: Lời cảm ơn
- **Audience move**: Close presentation

## X. Production Notes

- Vietnamese language throughout
- 12 slides fixed order per user structure
