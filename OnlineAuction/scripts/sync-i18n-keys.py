#!/usr/bin/env python3
"""Append localization keys to SharedResource .resx files."""
import re
from pathlib import Path

RESOURCES = Path(__file__).resolve().parent.parent / "Resources"

# key -> {culture: value}
KEYS = {
    "UserMenu_Orders": {"en-US": "Orders", "vi-VN": "Đơn hàng", "ja-JP": "注文", "ko-KR": "주문"},
    "UserMenu_Orders_Desc": {"en-US": "Track and view your purchases", "vi-VN": "Theo dõi và xem các giao dịch mua", "ja-JP": "購入履歴を確認", "ko-KR": "구매 내역 확인"},
    "UserMenu_Bids": {"en-US": "Bids", "vi-VN": "Đấu giá", "ja-JP": "入札", "ko-KR": "입찰"},
    "UserMenu_Bids_Desc": {"en-US": "View your active and past auctions bids", "vi-VN": "Xem các phiên đấu giá đang tham gia và đã qua", "ja-JP": "現在および過去の入札を表示", "ko-KR": "진행 중 및 과거 입찰 보기"},
    "UserMenu_Watchlist": {"en-US": "Watchlist", "vi-VN": "Danh sách theo dõi", "ja-JP": "ウォッチリスト", "ko-KR": "관심 목록"},
    "UserMenu_Watchlist_Desc": {"en-US": "Keep tabs on saved items", "vi-VN": "Theo dõi các mục đã lưu", "ja-JP": "保存した商品を追跡", "ko-KR": "저장한 상품 추적"},
    "UserMenu_Offers": {"en-US": "Offers", "vi-VN": "Đề nghị", "ja-JP": "オファー", "ko-KR": "제안"},
    "UserMenu_Offers_Desc": {"en-US": "Track offers you've made or received", "vi-VN": "Theo dõi đề nghị bạn đã gửi hoặc nhận", "ja-JP": "送信・受信したオファーを追跡", "ko-KR": "보낸/받은 제안 추적"},
    "UserMenu_Selling": {"en-US": "Selling", "vi-VN": "Đang bán", "ja-JP": "出品中", "ko-KR": "판매 중"},
    "UserMenu_Selling_Desc": {"en-US": "Monitor your active listings and sales", "vi-VN": "Theo dõi tin đăng và doanh số bán", "ja-JP": "出品と売上を管理", "ko-KR": "등록 상품 및 판매 모니터링"},
    "UserMenu_Summary": {"en-US": "Summary", "vi-VN": "Tổng quan", "ja-JP": "概要", "ko-KR": "요약"},
    "UserMenu_Summary_Desc": {"en-US": "An overall view of your account", "vi-VN": "Tổng quan tài khoản của bạn", "ja-JP": "アカウントの全体像", "ko-KR": "계정 전체 보기"},
    "UserMenu_Accounting": {"en-US": "Accounting", "vi-VN": "Kế toán", "ja-JP": "会計", "ko-KR": "회계"},
    "UserMenu_Accounting_Desc": {"en-US": "View balance, summaries, and transactions", "vi-VN": "Xem số dư, tổng hợp và giao dịch", "ja-JP": "残高・概要・取引を表示", "ko-KR": "잔액, 요약 및 거래 내역 보기"},
    "UserMenu_Submissions": {"en-US": "Submissions", "vi-VN": "Hồ sơ gửi", "ja-JP": "提出", "ko-KR": "제출"},
    "UserMenu_Submissions_Desc": {"en-US": "Review submitted items and progress", "vi-VN": "Xem các mục đã gửi và tiến độ", "ja-JP": "提出アイテムと進捗を確認", "ko-KR": "제출 항목 및 진행 상황 검토"},
    "UserMenu_VaultAddress": {"en-US": "My Vault address", "vi-VN": "Địa chỉ Vault của tôi", "ja-JP": "マイVaultアドレス", "ko-KR": "내 Vault 주소"},
    "UserMenu_CopyVault": {"en-US": "Copy vault address", "vi-VN": "Sao chép địa chỉ vault", "ja-JP": "Vaultアドレスをコピー", "ko-KR": "Vault 주소 복사"},
    "UserMenu_SubmitToVault": {"en-US": "Submit items to Vault", "vi-VN": "Gửi sản phẩm tới Vault", "ja-JP": "Vaultにアイテムを提出", "ko-KR": "Vault에 상품 제출"},
    "UserMenu_AccountMenu": {"en-US": "Account menu", "vi-VN": "Menu tài khoản", "ja-JP": "アカウントメニュー", "ko-KR": "계정 메뉴"},
    "Account_Wallet": {"en-US": "Wallet", "vi-VN": "Ví", "ja-JP": "ウォレット", "ko-KR": "지갑"},
    "Account_Orders": {"en-US": "Orders", "vi-VN": "Đơn hàng", "ja-JP": "注文", "ko-KR": "주문"},
    "Account_Bids": {"en-US": "Bids", "vi-VN": "Đấu giá", "ja-JP": "入札", "ko-KR": "입찰"},
    "Account_Watchlist": {"en-US": "Watchlist", "vi-VN": "Danh sách theo dõi", "ja-JP": "ウォッチリスト", "ko-KR": "관심 목록"},
    "Account_Offers": {"en-US": "Offers", "vi-VN": "Đề nghị", "ja-JP": "オファー", "ko-KR": "제안"},
    "Account_Selling": {"en-US": "Selling", "vi-VN": "Đang bán", "ja-JP": "出品中", "ko-KR": "판매 중"},
    "Account_Summary": {"en-US": "Summary", "vi-VN": "Tổng quan", "ja-JP": "概要", "ko-KR": "요약"},
    "Account_Accounting": {"en-US": "Accounting", "vi-VN": "Kế toán", "ja-JP": "会計", "ko-KR": "회계"},
    "Account_Submissions": {"en-US": "Submissions", "vi-VN": "Hồ sơ gửi", "ja-JP": "提出", "ko-KR": "제출"},
    "Account_Preferences": {"en-US": "Preferences", "vi-VN": "Tùy chọn", "ja-JP": "設定", "ko-KR": "환경설정"},
    "Account_Orders_Desc": {"en-US": "Track and view your purchases", "vi-VN": "Theo dõi và xem các giao dịch mua", "ja-JP": "購入履歴を確認", "ko-KR": "구매 내역 확인"},
    "Account_Bids_Desc": {"en-US": "View your active and past auction bids", "vi-VN": "Xem các phiên đấu giá đang tham gia và đã qua", "ja-JP": "現在および過去の入札を表示", "ko-KR": "진행 중 및 과거 입찰 보기"},
    "Account_Watchlist_Desc": {"en-US": "Keep tabs on saved items", "vi-VN": "Theo dõi các mục đã lưu", "ja-JP": "保存した商品を追跡", "ko-KR": "저장한 상품 추적"},
    "Account_Offers_Desc": {"en-US": "Track offers you've made or received", "vi-VN": "Theo dõi đề nghị bạn đã gửi hoặc nhận", "ja-JP": "送信・受信したオファーを追跡", "ko-KR": "보낸/받은 제안 추적"},
    "Account_Summary_Desc": {"en-US": "An overall view of your account", "vi-VN": "Tổng quan tài khoản của bạn", "ja-JP": "アカウントの全体像", "ko-KR": "계정 전체 보기"},
    "Account_Accounting_Desc": {"en-US": "View balance, summaries, and transactions", "vi-VN": "Xem số dư, tổng hợp và giao dịch", "ja-JP": "残高・概要・取引を表示", "ko-KR": "잔액, 요약 및 거래 내역 보기"},
    "Account_Submissions_Desc": {"en-US": "Review submitted items and progress", "vi-VN": "Xem các mục đã gửi và tiến độ", "ja-JP": "提出アイテムと進捗を確認", "ko-KR": "제출 항목 및 진행 상황 검토"},
    "Account_Preferences_Desc": {"en-US": "Manage your account settings and notifications", "vi-VN": "Quản lý cài đặt tài khoản và thông báo", "ja-JP": "アカウント設定と通知を管理", "ko-KR": "계정 설정 및 알림 관리"},
    "Account_Empty_Title": {"en-US": "Nothing here yet.", "vi-VN": "Chưa có nội dung.", "ja-JP": "まだ何もありません。", "ko-KR": "아직 내용이 없습니다."},
    "Account_Empty_Desc": {"en-US": "This section will show your {0} activity once you start using RareCard.", "vi-VN": "Phần này sẽ hiển thị hoạt động {0} khi bạn bắt đầu sử dụng RareCard.", "ja-JP": "RareCardの利用開始後、{0}のアクティビティがここに表示されます。", "ko-KR": "RareCard 사용을 시작하면 {0} 활동이 여기에 표시됩니다."},
    "Auth_Modal_Title": {"en-US": "Sign up or log in", "vi-VN": "Đăng ký hoặc đăng nhập", "ja-JP": "新規登録またはログイン", "ko-KR": "가입 또는 로그인"},
    "Auth_Modal_Subtitle": {"en-US": "Join the world's premier trading card auction vault", "vi-VN": "Tham gia kho đấu giá thẻ sưu tầm hàng đầu thế giới", "ja-JP": "世界最高峰のトレーディングカードオークションVaultへ", "ko-KR": "세계 최고의 트레이딩 카드 경매 Vault에 참여하세요"},
    "Auth_LogIn": {"en-US": "Log in", "vi-VN": "Đăng nhập", "ja-JP": "ログイン", "ko-KR": "로그인"},
    "Auth_SignUp": {"en-US": "Sign up", "vi-VN": "Đăng ký", "ja-JP": "新規登録", "ko-KR": "가입"},
    "Auth_Email": {"en-US": "Email", "vi-VN": "Email", "ja-JP": "メール", "ko-KR": "이메일"},
    "Auth_Password": {"en-US": "Password", "vi-VN": "Mật khẩu", "ja-JP": "パスワード", "ko-KR": "비밀번호"},
    "Auth_RememberMe": {"en-US": "Remember me", "vi-VN": "Ghi nhớ đăng nhập", "ja-JP": "ログイン状態を保持", "ko-KR": "로그인 유지"},
    "Auth_Continue": {"en-US": "Continue", "vi-VN": "Tiếp tục", "ja-JP": "続ける", "ko-KR": "계속"},
    "Auth_FullName": {"en-US": "Full name", "vi-VN": "Họ và tên", "ja-JP": "氏名", "ko-KR": "이름"},
    "Auth_Phone": {"en-US": "Phone", "vi-VN": "Số điện thoại", "ja-JP": "電話番号", "ko-KR": "전화번호"},
    "Auth_ConfirmPassword": {"en-US": "Confirm password", "vi-VN": "Xác nhận mật khẩu", "ja-JP": "パスワード確認", "ko-KR": "비밀번호 확인"},
    "Auth_CreateAccount": {"en-US": "Create Account", "vi-VN": "Tạo tài khoản", "ja-JP": "アカウント作成", "ko-KR": "계정 만들기"},
    "Auth_Legal": {"en-US": "By continuing, you agree to RareCard's Terms of Service and Privacy Policy.", "vi-VN": "Tiếp tục nghĩa là bạn đồng ý với Điều khoản dịch vụ và Chính sách bảo mật của RareCard.", "ja-JP": "続行することで、RareCardの利用規約とプライバシーポリシーに同意したものとみなされます。", "ko-KR": "계속하면 RareCard의 서비스 약관 및 개인정보 처리방침에 동의하게 됩니다."},
    "Auth_Close": {"en-US": "Close", "vi-VN": "Đóng", "ja-JP": "閉じる", "ko-KR": "닫기"},
    "Auth_Placeholder_Email": {"en-US": "Enter your email address", "vi-VN": "Nhập địa chỉ email", "ja-JP": "メールアドレスを入力", "ko-KR": "이메일 주소 입력"},
    "Auth_Placeholder_Password": {"en-US": "Enter your password", "vi-VN": "Nhập mật khẩu", "ja-JP": "パスワードを入力", "ko-KR": "비밀번호 입력"},
    "Auth_Placeholder_FullName": {"en-US": "Enter your full name", "vi-VN": "Nhập họ và tên", "ja-JP": "氏名を入力", "ko-KR": "이름 입력"},
    "Auth_Placeholder_Phone": {"en-US": "Enter your phone number", "vi-VN": "Nhập số điện thoại", "ja-JP": "電話番号を入力", "ko-KR": "전화번호 입력"},
    "Auth_Placeholder_SignupPassword": {"en-US": "Create a password", "vi-VN": "Tạo mật khẩu", "ja-JP": "パスワードを作成", "ko-KR": "비밀번호 생성"},
    "Auth_Placeholder_ConfirmPassword": {"en-US": "Confirm your password", "vi-VN": "Xác nhận mật khẩu", "ja-JP": "パスワードを確認", "ko-KR": "비밀번호 확인"},
    "Auth_Slide1_Eyebrow": {"en-US": "RareCard Vault", "vi-VN": "Kho RareCard", "ja-JP": "RareCard Vault", "ko-KR": "RareCard Vault"},
    "Auth_Slide1_Title": {"en-US": "Welcome to RareCard", "vi-VN": "Chào mừng đến RareCard", "ja-JP": "RareCardへようこそ", "ko-KR": "RareCard에 오신 것을 환영합니다"},
    "Auth_Slide1_Desc": {"en-US": "Bid on authenticated graded cards from PSA, BGS & CGC vaults.", "vi-VN": "Đấu giá thẻ đã xác thực từ các kho PSA, BGS và CGC.", "ja-JP": "PSA、BGS、CGCのVaultから認証済みグレードカードに入札。", "ko-KR": "PSA, BGS, CGC Vault의 인증 등급 카드에 입찰하세요."},
    "Auth_Slide2_Eyebrow": {"en-US": "Pokémon & TCG", "vi-VN": "Pokémon & TCG", "ja-JP": "ポケモン & TCG", "ko-KR": "포켓몬 & TCG"},
    "Auth_Slide2_Title": {"en-US": "Discover Rare Holos", "vi-VN": "Khám phá Holo hiếm", "ja-JP": "レアホロを発見", "ko-KR": "희귀 홀로 발견"},
    "Auth_Slide2_Desc": {"en-US": "Base Set Charizards, Illustrator promos & manga rare parallels.", "vi-VN": "Charizard Base Set, Illustrator promo và manga rare parallels.", "ja-JP": "ベースセットリザードン、イラストレータープロモなど。", "ko-KR": "베이스 세트 리자몽, 일러스트레이터 프로모 등."},
    "Auth_Slide3_Eyebrow": {"en-US": "Sports & MTG", "vi-VN": "Thể thao & MTG", "ja-JP": "スポーツ & MTG", "ko-KR": "스포츠 & MTG"},
    "Auth_Slide3_Title": {"en-US": "Legends on Auction", "vi-VN": "Huyền thoại trên đấu giá", "ja-JP": "伝説のオークション", "ko-KR": "경매의 전설"},
    "Auth_Slide3_Desc": {"en-US": "From Mickey Mantle rookies to Alpha Black Lotus — curated daily.", "vi-VN": "Từ Mickey Mantle rookie đến Alpha Black Lotus — tuyển chọn mỗi ngày.", "ja-JP": "ミッキーマンテルからAlpha Black Lotusまで毎日厳選。", "ko-KR": "Mickey Mantle부터 Alpha Black Lotus까지 매일 엄선."},
    "Layout_Footer_AboutUs": {"en-US": "About Us", "vi-VN": "Về chúng tôi", "ja-JP": "会社概要", "ko-KR": "회사 소개"},
    "Layout_Footer_ContactUs": {"en-US": "Contact Us", "vi-VN": "Liên hệ", "ja-JP": "お問い合わせ", "ko-KR": "문의하기"},
    "Layout_Footer_PolicyTerms": {"en-US": "Policy & Term of Use", "vi-VN": "Chính sách & Điều khoản", "ja-JP": "ポリシーと利用規約", "ko-KR": "정책 및 이용약관"},
    "Layout_Copyright": {"en-US": "© {0} RareCard. All rights reserved.", "vi-VN": "© {0} RareCard. Bảo lưu mọi quyền.", "ja-JP": "© {0} RareCard. All rights reserved.", "ko-KR": "© {0} RareCard. All rights reserved."},
    "Auction_PageTitle": {"en-US": "Live Auctions", "vi-VN": "Đấu giá trực tiếp", "ja-JP": "ライブオークション", "ko-KR": "라이브 경매"},
    "Auction_Subtitle": {"en-US": "Discover authenticated, high-grade collectibles curated for the world's most discerning collectors.", "vi-VN": "Khám phá vật phẩm sưu tầm đã xác thực, chất lượng cao dành cho nhà sưu tầm khó tính.", "ja-JP": "世界最高峰のコレクター向けに厳選された認証済み高品質コレクション。", "ko-KR": "세계 최고의 수집가를 위해 엄선된 인증 고급 수집품."},
    "Auction_LiveNow": {"en-US": "Live now", "vi-VN": "Đang diễn ra", "ja-JP": "ライブ中", "ko-KR": "진행 중"},
    "Auction_ActiveBids": {"en-US": "Active Bids ({0})", "vi-VN": "Đấu giá đang tham gia ({0})", "ja-JP": "アクティブ入札 ({0})", "ko-KR": "진행 중 입찰 ({0})"},
    "Auction_WatchlistCount": {"en-US": "Watchlist ({0})", "vi-VN": "Theo dõi ({0})", "ja-JP": "ウォッチリスト ({0})", "ko-KR": "관심 목록 ({0})"},
    "Auction_Filter_Category": {"en-US": "Category", "vi-VN": "Danh mục", "ja-JP": "カテゴリー", "ko-KR": "카테고리"},
    "Auction_Filter_Condition": {"en-US": "Condition", "vi-VN": "Tình trạng", "ja-JP": "状態", "ko-KR": "상태"},
    "Auction_Filter_Year": {"en-US": "Year Manufactured", "vi-VN": "Năm sản xuất", "ja-JP": "製造年", "ko-KR": "제조 연도"},
    "Auction_Filter_PriceRange": {"en-US": "Price Range", "vi-VN": "Khoảng giá", "ja-JP": "価格帯", "ko-KR": "가격 범위"},
    "Auction_Filter_EndingSoonOnly": {"en-US": "Ending Soon Only", "vi-VN": "Chỉ sắp kết thúc", "ja-JP": "まもなく終了のみ", "ko-KR": "곧 종료만"},
    "Auction_ClearFilters": {"en-US": "Clear filters", "vi-VN": "Xóa bộ lọc", "ja-JP": "フィルターをクリア", "ko-KR": "필터 초기화"},
    "Auction_ShowingItems": {"en-US": "Showing {0} items", "vi-VN": "Hiển thị {0} mục", "ja-JP": "{0}件を表示", "ko-KR": "{0}개 표시"},
    "Auction_Sort_Featured": {"en-US": "Featured", "vi-VN": "Nổi bật", "ja-JP": "おすすめ", "ko-KR": "추천"},
    "Auction_Sort_EndingSoon": {"en-US": "Ending soon", "vi-VN": "Sắp kết thúc", "ja-JP": "まもなく終了", "ko-KR": "곧 종료"},
    "Auction_Sort_PriceAsc": {"en-US": "Price: low to high", "vi-VN": "Giá: thấp đến cao", "ja-JP": "価格: 安い順", "ko-KR": "가격: 낮은순"},
    "Auction_Sort_PriceDesc": {"en-US": "Price: high to low", "vi-VN": "Giá: cao đến thấp", "ja-JP": "価格: 高い順", "ko-KR": "가격: 높은순"},
    "Auction_Sort_Name": {"en-US": "Name A–Z", "vi-VN": "Tên A–Z", "ja-JP": "名前 A–Z", "ko-KR": "이름 A–Z"},
    "Auction_Condition_Graded": {"en-US": "Graded", "vi-VN": "Đã chấm điểm", "ja-JP": "グレード済み", "ko-KR": "등급"},
    "Auction_Condition_NotSpecified": {"en-US": "Not Specified", "vi-VN": "Không xác định", "ja-JP": "未指定", "ko-KR": "미지정"},
    "Card_CurrentBid": {"en-US": "Current Bid", "vi-VN": "Giá hiện tại", "ja-JP": "現在の入札", "ko-KR": "현재 입찰가"},
    "Card_TimeLeft": {"en-US": "Time Left", "vi-VN": "Thời gian còn lại", "ja-JP": "残り時間", "ko-KR": "남은 시간"},
    "Card_BidNow": {"en-US": "Bid Now", "vi-VN": "Đấu giá ngay", "ja-JP": "入札する", "ko-KR": "입찰하기"},
    "Card_AddToCart": {"en-US": "Add to Cart", "vi-VN": "Thêm vào giỏ", "ja-JP": "カートに追加", "ko-KR": "장바구니 추가"},
    "Card_WinningBid": {"en-US": "Winning Bid", "vi-VN": "Giá thắng", "ja-JP": "落札価格", "ko-KR": "낙찰가"},
    "Card_CompletePayment": {"en-US": "Complete Payment", "vi-VN": "Hoàn tất thanh toán", "ja-JP": "支払いを完了", "ko-KR": "결제 완료"},
    "Card_PlaceBid": {"en-US": "Place Bid", "vi-VN": "Đặt giá", "ja-JP": "入札", "ko-KR": "입찰"},
    "Card_EndingSoon": {"en-US": "Ending Soon", "vi-VN": "Sắp kết thúc", "ja-JP": "まもなく終了", "ko-KR": "곧 종료"},
    "Card_Hot": {"en-US": "Hot", "vi-VN": "Hot", "ja-JP": "Hot", "ko-KR": "Hot"},
    "Order_PageTitle": {"en-US": "Won Auctions", "vi-VN": "Phiên đấu giá đã thắng", "ja-JP": "落札オークション", "ko-KR": "낙찰 경매"},
    "Order_ItemsWon": {"en-US": "{0} Item(s) Won", "vi-VN": "Đã thắng {0} mục", "ja-JP": "{0}件落札", "ko-KR": "{0}개 낙찰"},
    "Order_Empty_Title": {"en-US": "No won auctions yet", "vi-VN": "Chưa có phiên đấu giá thắng", "ja-JP": "落札はまだありません", "ko-KR": "아직 낙찰 경매 없음"},
    "Order_Empty_Desc": {"en-US": "Place a winning bid on any live auction and your item will appear here for checkout.", "vi-VN": "Thắng một phiên đấu giá và sản phẩm sẽ xuất hiện tại đây để thanh toán.", "ja-JP": "ライブオークションで落札すると、ここにチェックアウト用に表示されます。", "ko-KR": "라이브 경매에서 낙찰하면 결제를 위해 여기에 표시됩니다."},
    "Order_BrowseAuctions": {"en-US": "Browse Auctions", "vi-VN": "Xem đấu giá", "ja-JP": "オークションを見る", "ko-KR": "경매 둘러보기"},
    "Order_PaymentDeadline": {"en-US": "Payment Deadline:", "vi-VN": "Hạn thanh toán:", "ja-JP": "支払期限:", "ko-KR": "결제 기한:"},
    "BuyNow_PageTitle": {"en-US": "Buy Now", "vi-VN": "Mua ngay", "ja-JP": "即購入", "ko-KR": "즉시 구매"},
    "BuyNow_Subtitle": {"en-US": "Instantly purchase authenticated collectibles at fixed prices.", "vi-VN": "Mua ngay vật phẩm sưu tầm đã xác thực với giá cố định.", "ja-JP": "認証済みコレクションを定価で即購入。", "ko-KR": "인증된 수집품을 고정가로 즉시 구매."},
    "BuyNow_AvailableNow": {"en-US": "Available now", "vi-VN": "Có sẵn", "ja-JP": "在庫あり", "ko-KR": "구매 가능"},
    "User_SellerProfile": {"en-US": "Seller Profile", "vi-VN": "Hồ sơ người bán", "ja-JP": "出品者プロフィール", "ko-KR": "판매자 프로필"},
    "User_BasicInformation": {"en-US": "Basic Information", "vi-VN": "Thông tin cơ bản", "ja-JP": "基本情報", "ko-KR": "기본 정보"},
    "User_BasicInformation_Desc": {"en-US": "Contact and profile details", "vi-VN": "Thông tin liên hệ và hồ sơ", "ja-JP": "連絡先とプロフィール詳細", "ko-KR": "연락처 및 프로필 정보"},
    "User_FullName": {"en-US": "Full Name", "vi-VN": "Họ và tên", "ja-JP": "氏名", "ko-KR": "이름"},
    "User_Email": {"en-US": "Email", "vi-VN": "Email", "ja-JP": "メール", "ko-KR": "이메일"},
    "User_PhoneNumber": {"en-US": "Phone Number", "vi-VN": "Số điện thoại", "ja-JP": "電話番号", "ko-KR": "전화번호"},
    "User_Address": {"en-US": "Address", "vi-VN": "Địa chỉ", "ja-JP": "住所", "ko-KR": "주소"},
    "User_MemberSince": {"en-US": "Member since {0}", "vi-VN": "Thành viên từ {0}", "ja-JP": "{0}年から会員", "ko-KR": "{0}년부터 회원"},
    "Contact_PageTitle": {"en-US": "Contact Us", "vi-VN": "Liên hệ", "ja-JP": "お問い合わせ", "ko-KR": "문의하기"},
    "Payment_PageTitle": {"en-US": "Payment Information", "vi-VN": "Thông tin thanh toán", "ja-JP": "支払い情報", "ko-KR": "결제 정보"},
    "Payment_AddMethod": {"en-US": "Add New Payment Method", "vi-VN": "Thêm phương thức thanh toán", "ja-JP": "新しい支払い方法を追加", "ko-KR": "새 결제 수단 추가"},
    "Common_Search": {"en-US": "Search", "vi-VN": "Tìm kiếm", "ja-JP": "検索", "ko-KR": "검색"},
    "Common_Breadcrumb_Home": {"en-US": "Home", "vi-VN": "Trang chủ", "ja-JP": "ホーム", "ko-KR": "홈"},
    "Layout_ToggleNavigation": {"en-US": "Toggle navigation", "vi-VN": "Mở/đóng menu", "ja-JP": "ナビゲーション切替", "ko-KR": "내비게이션 토글"},
    "Layout_WonAuctions": {"en-US": "Won auctions", "vi-VN": "Phiên đã thắng", "ja-JP": "落札オークション", "ko-KR": "낙찰 경매"},
    "Product_AuthenticatedListing": {"en-US": "Authenticated listing", "vi-VN": "Tin đăng đã xác thực", "ja-JP": "認証済み出品", "ko-KR": "인증된 등록"},
    "Product_CurrentBid": {"en-US": "Current bid", "vi-VN": "Giá hiện tại", "ja-JP": "現在の入札", "ko-KR": "현재 입찰가"},
    "Product_PlaceMaxBid": {"en-US": "Place max bid", "vi-VN": "Đặt giá tối đa", "ja-JP": "最大入札", "ko-KR": "최대 입찰"},
    "Product_Bid": {"en-US": "Bid", "vi-VN": "Đấu giá", "ja-JP": "入札", "ko-KR": "입찰"},
    "Product_AboutTheWork": {"en-US": "About the work", "vi-VN": "Giới thiệu", "ja-JP": "作品について", "ko-KR": "작품 소개"},
    "Product_Grading": {"en-US": "Grading", "vi-VN": "Chấm điểm", "ja-JP": "グレーディング", "ko-KR": "등급"},
    "Product_BidHistory": {"en-US": "Bid history", "vi-VN": "Lịch sử đấu giá", "ja-JP": "入札履歴", "ko-KR": "입찰 내역"},
    "Product_Shipping": {"en-US": "Shipping", "vi-VN": "Vận chuyển", "ja-JP": "配送", "ko-KR": "배송"},
    "Product_EstimatedValue": {"en-US": "Estimated value:", "vi-VN": "Giá trị ước tính:", "ja-JP": "推定価値:", "ko-KR": "예상 가치:"},
    "Product_BuyersPremium": {"en-US": "Price includes buyer's premium. Shipping and taxes may apply.", "vi-VN": "Giá đã bao gồm phí người mua. Phí vận chuyển và thuế có thể áp dụng.", "ja-JP": "価格にはバイヤーズプレミアムが含まれます。送料と税金が適用される場合があります。", "ko-KR": "가격에 구매자 수수료가 포함됩니다. 배송비 및 세금이 적용될 수 있습니다."},
    "Product_Lot": {"en-US": "Lot {0}", "vi-VN": "Lot {0}", "ja-JP": "Lot {0}", "ko-KR": "Lot {0}"},
    "Product_EndsAt": {"en-US": "Ends {0}", "vi-VN": "Kết thúc {0}", "ja-JP": "{0}に終了", "ko-KR": "{0} 종료"},
    "Product_ClosureNote": {"en-US": "*Closure times may be extended to accommodate last-minute bids", "vi-VN": "*Thời gian kết thúc có thể được gia hạn cho các lượt đấu giá phút chót", "ja-JP": "*終了時刻はラストミニット入札により延長される場合があります", "ko-KR": "*마감 시간은 막판 입찰에 따라 연장될 수 있습니다"},
    "Product_BidsCount": {"en-US": "{0} bid(s)", "vi-VN": "{0} lượt đấu giá", "ja-JP": "{0}件の入札", "ko-KR": "입찰 {0}건"},
    "Product_ReserveMet": {"en-US": "reserve met", "vi-VN": "đạt giá dự trữ", "ja-JP": "リザーブ達成", "ko-KR": "예약가 충족"},
    "Product_WatchersCount": {"en-US": "{0} watchers", "vi-VN": "{0} người theo dõi", "ja-JP": "{0}人がウォッチ中", "ko-KR": "관심 {0}명"},
    "Product_CountdownIn": {"en-US": "in {0}d {1}h", "vi-VN": "còn {0} ngày {1} giờ", "ja-JP": "残り{0}日{1}時間", "ko-KR": "{0}일 {1}시간 남음"},
    "RelatedProducts_Title": {"en-US": "Related Products", "vi-VN": "Sản phẩm liên quan", "ja-JP": "関連商品", "ko-KR": "관련 상품"},
    "Faq_PageTitle": {"en-US": "FAQ", "vi-VN": "Câu hỏi thường gặp", "ja-JP": "FAQ", "ko-KR": "FAQ"},
    "AboutUs_PageTitle": {"en-US": "About Us", "vi-VN": "Về chúng tôi", "ja-JP": "会社概要", "ko-KR": "회사 소개"},
    "Policy_PageTitle": {"en-US": "Policy & Terms", "vi-VN": "Chính sách & Điều khoản", "ja-JP": "ポリシーと規約", "ko-KR": "정책 및 약관"},
    "Refund_PageTitle": {"en-US": "Refund Request", "vi-VN": "Yêu cầu hoàn tiền", "ja-JP": "返金リクエスト", "ko-KR": "환불 요청"},
    "Error_PageTitle": {"en-US": "Error", "vi-VN": "Lỗi", "ja-JP": "エラー", "ko-KR": "오류"},
    "Error_Description": {"en-US": "An error occurred while processing your request.", "vi-VN": "Đã xảy ra lỗi khi xử lý yêu cầu của bạn.", "ja-JP": "リクエストの処理中にエラーが発生しました。", "ko-KR": "요청 처리 중 오류가 발생했습니다."},
}


def escape_xml(text: str) -> str:
    return (
        text.replace("&", "&amp;")
        .replace("<", "&lt;")
        .replace(">", "&gt;")
        .replace('"', "&quot;")
    )


def make_entry(name: str, value: str) -> str:
    return f'  <data name="{name}" xml:space="preserve"><value>{escape_xml(value)}</value></data>\n'


def append_keys(path: Path, culture: str | None):
    content = path.read_text(encoding="utf-8")
    existing = set(re.findall(r'<data name="([^"]+)"', content))
    entries = []
    for key, translations in KEYS.items():
        if key in existing:
            continue
        value = translations["en-US"] if culture is None else translations.get(culture, translations["en-US"])
        entries.append(make_entry(key, value))
    if not entries:
        print(f"No new keys for {path.name}")
        return
    content = content.replace("</root>", "".join(entries) + "</root>")
    path.write_text(content, encoding="utf-8")
    print(f"Added {len(entries)} keys to {path.name}")


def main():
    append_keys(RESOURCES / "SharedResource.resx", None)
    for culture in ("en-US", "vi-VN", "ja-JP", "ko-KR"):
        append_keys(RESOURCES / f"SharedResource.{culture}.resx", culture)


if __name__ == "__main__":
    main()
