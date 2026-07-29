using ClosedXML.Excel;

var outputPath = args.Length > 0 && !args[0].StartsWith('-')
    ? args[0]
    : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "docs", "OnlineAuction_spec_test.xlsx"));

var skipTests = args.Contains("--skip-tests");

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

TestRunSummary? testSummary = null;
if (!skipTests)
{
    var testProject = ResolveRepoFile("OnlineAuction.Tests", "OnlineAuction.Tests.csproj");
    Console.WriteLine("Running dotnet test to collect results...");
    testSummary = TestResultsCollector.Collect(testProject);
    Console.WriteLine($"Tests: {testSummary.Passed}/{testSummary.Total} passed, {testSummary.Failed} failed.");
}

using var workbook = new XLWorkbook();
BuildModuleIndexSheet(workbook);
BuildSpecSheet(workbook, testSummary);
BuildTestDataPatternSheet(workbook);
BuildAutomatedTestsSheet(workbook, testSummary);
BuildRemarksSheet(workbook);

workbook.SaveAs(outputPath);
Console.WriteLine($"Saved: {outputPath}");

static string ResolveRepoFile(params string[] relativeSegments)
{
    var dir = AppContext.BaseDirectory;
    for (var depth = 0; depth < 12 && !string.IsNullOrEmpty(dir); depth++)
    {
        var candidate = Path.GetFullPath(Path.Combine(new[] { dir }.Concat(relativeSegments).ToArray()));
        if (File.Exists(candidate))
        {
            return candidate;
        }

        dir = Path.GetDirectoryName(dir)!;
    }

    throw new FileNotFoundException(
        $"Could not locate {string.Join(Path.DirectorySeparatorChar, relativeSegments)} from {AppContext.BaseDirectory}");
}

static void StyleHeader(IXLRange range)
{
    range.Style.Font.SetBold();
    range.Style.Fill.BackgroundColor = XLColor.FromHtml("#D9E1F2");
}

static void BuildModuleIndexSheet(XLWorkbook workbook)
{
    var ws = workbook.Worksheets.Add("module index");
    ws.Cell(1, 1).Value = Bilingual.Bi("Module", "Module");
    ws.Cell(1, 2).Value = Bilingual.Bi("Area / Routes", "Khu vực / Route");
    ws.Cell(1, 3).Value = Bilingual.Bi("Test IDs (Type)", "Mã test (Loại)");
    ws.Cell(1, 4).Value = Bilingual.Bi("Automated coverage", "Coverage tự động");
    StyleHeader(ws.Range(1, 1, 1, 4));

    var rows = new (string ModuleEn, string ModuleVi, string Area, string Ids, string Auto)[]
    {
        ("Setup", "Thiết lập", "MySQL, EF migrate, seed, localhost:5006", "SETUP-01..03", "—"),
        ("Auth (Public)", "Auth (Public)", "/Auth/Login, SignUp, Logout", "AUTH-01..08, AUTH-REG-01, AUTH-LOGIN-01", "Smoke script"),
        ("Auth (Admin)", "Auth (Admin)", "/Admin/Account/Login, dual session", "ADM-AUTH-01..05", "Manual"),
        ("Release smoke", "Smoke release", "Pre-merge gate pack", "AUTH-REG-01, AUTH-LOGIN-01, AUCTION_REG-03, BID-01", "Invoke-ReleaseSmoke.ps1"),
        ("Auction catalog", "Catalog auction", "/, /Auction, visibility", "CAT-01..08", "AuctionVisibilityTests, ConfirmingStatusTests"),
        ("Auction detail & Bid", "Detail & Bid", "/Auction/Detail/{id}, PlaceBid", "BID-01..12", "BidServicePlaceBidTests, BidIncrementValidationTests"),
        ("Registration & Deposit", "Đăng ký & Cọc", "Auction registration + PayPal deposit", "AUCTION_REG-01..06, AUCTION_REG-03", "PayPalCaptureFlowTests (deposit)"),
        ("Buy Now", "Buy Now", "/BuyNow, purchase flow", "BN-01..06", "Partial"),
        ("Sell (Seller)", "Sell (Seller)", "/Sell/Create, /Sell/BuyNow", "SELL-01..08", "Partial"),
        ("Admin listing forms", "Form listing Admin", "/Admin/Auction/Create*", "ADM-SYNC-01..08", "AdminAuctionFormSyncTests"),
        ("Auction verification", "Duyệt listing", "/Admin/AuctionVerification", "VERIFY-01..08", "ConfirmingStatusTests"),
        ("Admin dashboard", "Dashboard Admin", "/Admin/Dashboard", "DASH-01..06", "DashboardFilterValidatorTests"),
        ("Admin CRUD", "Admin CRUD", "Categories, Users, Products, Permissions", "ADM-CRUD-01..08", "Partial"),
        ("Order / Payment Center", "Order / Thanh toán", "/Order, checkout selection", "ORD-01..10", "OrderCheckoutSelectionTests, OrderPayPathFeeTests"),
        ("PayPal checkout", "Checkout PayPal", "/Payment/PayPalReturn, capture", "PAY-01..08", "PayPalCaptureFlowTests"),
        ("Winner non-payment", "Winner không trả", "48h expiry, second chance", "WNP-01..05", "WinnerNonPaymentRecoveryIntegrationTests"),
        ("Fees & proceeds", "Phí & proceeds", "Registration, checkout, seller fees", "FEE-01..05", "MarketplaceFeeCalculatorTests, ListingFeeCalculatorTests"),
        ("Product documents", "Tài liệu sản phẩm", "Upload, download, auth", "DOC-01..06", "ProductDocumentDownloadTests"),
        ("Confirming gates", "Chặn confirming", "Hidden listing rules", "CONF-01..06", "ConfirmingStatusTests"),
        ("Watchlist", "Watchlist", "/Watchlist", "WATCH-01..03", "ConfirmingStatusTests"),
        ("Notifications", "Thông báo", "In-app dropdown", "NOTIF-01..03", "Partial"),
        ("Bid rate limit / fraud", "Rate limit / gian lận", "429, challenge, shadow ban", "FRAUD-01..04", "BidRateLimitServiceTests"),
        ("Static / Contact", "Trang tĩnh / Contact", "/Contact, /AboutUs, /Faq, /Policy", "PAGE-01..05", "Manual"),
        ("Regression", "Regression", "Full suite", "AUTO-01..03", "dotnet test (70 tests)"),
    };

    for (var i = 0; i < rows.Length; i++)
    {
        var r = i + 2;
        ws.Cell(r, 1).Value = Bilingual.Bi(rows[i].ModuleEn, rows[i].ModuleVi);
        ws.Cell(r, 2).Value = rows[i].Area;
        ws.Cell(r, 3).Value = rows[i].Ids;
        ws.Cell(r, 4).Value = rows[i].Auto;
    }

    ws.Columns().AdjustToContents();
    ws.SheetView.FreezeRows(1);
}

static void BuildSpecSheet(XLWorkbook workbook, TestRunSummary? testSummary)
{
    var ws = workbook.Worksheets.Add("spec test");

    ws.Cell(1, 1).Value = Bilingual.Bi("Project", "Dự án");
    ws.Cell(1, 3).Value = Bilingual.Bi(
        "T2502E_Nhom3_OnlineAuction — Online Auction Platform (full project)",
        "T2502E_Nhom3_OnlineAuction — Nền tảng đấu giá trực tuyến (toàn dự án)");
    ws.Cell(2, 1).Value = Bilingual.Bi("Module", "Module");
    ws.Cell(2, 3).Value = Bilingual.Bi(
        "All modules: Auth, Auction, Bid, Buy Now, Sell, Admin, Order, PayPal, Verification",
        "Tất cả module: Auth, Auction, Bid, Buy Now, Sell, Admin, Order, PayPal, Verification");
    ws.Cell(2, 8).Value = "IT";
    ws.Cell(2, 12).Value = Bilingual.Bi("ST / E2E", "ST / E2E");

    ws.Range(6, 1, 6, 12).Merge();
    ws.Cell(6, 1).Value = Bilingual.Bi(
        "Test specification — entire OnlineAuction project (manual + automated mapping)",
        "Đặc tả test — toàn bộ dự án OnlineAuction (manual + mapping tự động)");

    const int headerRow = 7;
    const int colActual = 22;
    const int colOwner = 28;
    const int colExecDate = 32;
    const int colResult = 36;
    const int colDoneDate = 39;
    const int colNotes = 44;

    ws.Cell(headerRow, 1).Value = "No.";
    ws.Cell(headerRow, 3).Value = Bilingual.Bi("Type", "Loại");
    ws.Cell(headerRow, 6).Value = Bilingual.Bi("Test item", "Mục test");
    ws.Cell(headerRow, 10).Value = Bilingual.Bi("Test conditions", "Điều kiện test");
    ws.Cell(headerRow, 16).Value = Bilingual.Bi("Expected result", "Kết quả dự kiến");
    ws.Cell(headerRow, colActual).Value = Bilingual.Bi("Actual result", "Kết quả thực tế");
    ws.Cell(headerRow, colOwner).Value = Bilingual.Bi("Owner", "Người chịu trách nhiệm");
    ws.Cell(headerRow, colExecDate).Value = Bilingual.Bi("Test execution date", "Ngày thực thi test");
    ws.Cell(headerRow, colResult).Value = Bilingual.Bi("Result (OK/NG)", "Kết quả (OK/NG)");
    ws.Cell(headerRow, colDoneDate).Value = Bilingual.Bi("Test completion date", "Ngày hoàn thành test");
    ws.Cell(headerRow, colNotes).Value = Bilingual.Bi("Notes", "Ghi chú");
    StyleHeader(ws.Range(headerRow, 1, headerRow, colNotes));
    ws.Row(headerRow).Style.Alignment.WrapText = true;

    var runDate = testSummary?.RunAtUtc.ToLocalTime() ?? DateTime.Now;

    var cases = TestCasesData.All;
    var row = headerRow + 1;
    foreach (var tc in cases)
    {
        ws.Cell(row, 1).Value = tc.No;
        ws.Cell(row, 3).Value = tc.Type;
        ws.Cell(row, 6).Value = Bilingual.Bi(tc.ItemEn, tc.ItemVi);
        ws.Cell(row, 10).Value = Bilingual.Bi(tc.ConditionEn, tc.ConditionVi);
        ws.Cell(row, 16).Value = Bilingual.Bi(tc.ExpectedEn, tc.ExpectedVi);
        ws.Range(row, 6, row, 16).Style.Alignment.WrapText = true;

        ApplyManualResults(ws, row, tc.Type, runDate, colActual, colOwner, colExecDate, colResult, colDoneDate, colNotes);

        if (testSummary != null)
        {
            ApplyTestResults(ws, row, tc.Type, testSummary, runDate, colActual, colOwner, colExecDate, colResult, colDoneDate, colNotes);
        }

        row++;
    }

    var resultRange = ws.Range(headerRow + 1, colResult, row - 1, colResult);
    resultRange.CreateDataValidation().List("\"OK,NG,N/A,Pending\"", true);
    resultRange.Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

    ws.Range(headerRow + 1, colExecDate, row - 1, colExecDate).Style.DateFormat.Format = "yyyy/mm/dd";
    ws.Range(headerRow + 1, colDoneDate, row - 1, colDoneDate).Style.DateFormat.Format = "yyyy/mm/dd";

    ws.Columns(1, colNotes).AdjustToContents(1, row);
    ws.Column(colResult).Width = 10;
    ws.SheetView.FreezeRows(headerRow);
}

static void ApplyManualResults(
    IXLWorksheet ws,
    int row,
    string type,
    DateTime runDate,
    int colActual,
    int colOwner,
    int colExecDate,
    int colResult,
    int colDoneDate,
    int colNotes)
{
    if (!ManualVerificationMap.Verified.TryGetValue(type, out var manual))
        return;

    ws.Cell(row, colOwner).Value = Bilingual.Bi("Manual", "Manual");
    ws.Cell(row, colExecDate).Value = runDate;
    ws.Cell(row, colDoneDate).Value = runDate;
    ws.Cell(row, colActual).Value = Bilingual.Bi(manual.ActualEn, manual.ActualVi);
    ws.Cell(row, colResult).Value = "OK";
    ws.Cell(row, colNotes).Value = Bilingual.Bi(manual.NotesEn, manual.NotesVi);
}

static void ApplyTestResults(
    IXLWorksheet ws,
    int row,
    string type,
    TestRunSummary summary,
    DateTime runDate,
    int colActual,
    int colOwner,
    int colExecDate,
    int colResult,
    int colDoneDate,
    int colNotes)
{
    if (ManualVerificationMap.Verified.ContainsKey(type))
        return;

    ws.Cell(row, colOwner).Value = "Automated";
    ws.Cell(row, colExecDate).Value = runDate;
    ws.Cell(row, colDoneDate).Value = runDate;

    if (type == "AUTO-01")
    {
        var actualEn = $"{summary.Passed}/{summary.Total} passed";
        var actualVi = $"{summary.Passed}/{summary.Total} pass";
        ws.Cell(row, colActual).Value = Bilingual.Bi(actualEn, actualVi);
        ws.Cell(row, colResult).Value = summary.Success ? "OK" : "NG";
        ws.Cell(row, colNotes).Value = Bilingual.Bi(
            summary.Failed > 0 ? $"{summary.Failed} test(s) failed — see dotnet test output" : "dotnet test OnlineAuction.Tests",
            summary.Failed > 0 ? $"{summary.Failed} test fail — xem dotnet test output" : "dotnet test OnlineAuction.Tests");
        return;
    }

    if (type == "AUTO-02")
    {
        if (TestResultsCollector.TryGetClassResult(summary, "AdminAuctionFormSyncTests", out var cls))
        {
            var total = cls.Passed + cls.Failed + cls.Skipped;
            ws.Cell(row, colActual).Value = Bilingual.Bi(
                $"{cls.Passed}/{total} passed (AdminAuctionFormSyncTests)",
                $"{cls.Passed}/{total} pass (AdminAuctionFormSyncTests)");
            ws.Cell(row, colResult).Value = cls.Success ? "OK" : "NG";
        }
        else
        {
            ws.Cell(row, colActual).Value = Bilingual.Bi("Class not found in TRX", "Không tìm thấy class trong TRX");
            ws.Cell(row, colResult).Value = "NG";
        }

        ws.Cell(row, colNotes).Value = Bilingual.Bi(
            "Filter: AdminAuctionFormSyncTests",
            "Filter: AdminAuctionFormSyncTests");
        return;
    }

    if (type == "AUTO-03")
    {
        ws.Cell(row, colActual).Value = Bilingual.Bi("Manual — not run", "Manual — chưa chạy");
        ws.Cell(row, colResult).Value = "Pending";
        ws.Cell(row, colNotes).Value = Bilingual.Bi(
            "Run .\\scripts\\smoke\\Invoke-ReleaseSmoke.ps1 manually",
            "Chạy thủ công .\\scripts\\smoke\\Invoke-ReleaseSmoke.ps1");
        return;
    }

    var classes = AutomatedCoverageMap.GetClassesForType(type);
    if (classes == null)
        return;

    if (AutomatedCoverageMap.AllClassesPassed(summary, classes))
    {
        var classList = string.Join(", ", classes);
        var counts = string.Join("; ", classes.Select(c =>
            summary.Classes.TryGetValue(c, out var r) ? $"{c}: {r.Passed} pass" : $"{c}: n/a"));

        ws.Cell(row, colActual).Value = Bilingual.Bi(
            $"Automated coverage passed ({counts})",
            $"Coverage tự động pass ({counts})");
        ws.Cell(row, colResult).Value = "OK";
        ws.Cell(row, colNotes).Value = Bilingual.Bi(
            $"Covered by: {classList}",
            $"Coverage: {classList}");
    }
    else
    {
        ws.Cell(row, colActual).Value = Bilingual.Bi(
            "Automated tests failed for mapped class(es)",
            "Test tự động fail cho class được map");
        ws.Cell(row, colResult).Value = "NG";
        ws.Cell(row, colNotes).Value = Bilingual.Bi(
            $"Check: {string.Join(", ", classes)}",
            $"Kiểm tra: {string.Join(", ", classes)}");
    }
}

static void WriteBilingualHeaders(IXLWorksheet ws, int row, (string En, string Vi)[] headers)
{
    for (var c = 0; c < headers.Length; c++)
        ws.Cell(row, c + 1).Value = Bilingual.Bi(headers[c].En, headers[c].Vi);
}

static void BuildTestDataPatternSheet(XLWorkbook workbook)
{
    var ws = workbook.Worksheets.Add("test data pattern");

    ws.Cell(1, 1).Value = Bilingual.Bi("=== Test accounts (after UserSeeder) ===", "=== Tài khoản test (sau UserSeeder) ===");
    var accountHeaders = new (string En, string Vi)[]
    {
        ("No", "STT"), ("Email", "Email"), ("Password", "Mật khẩu"), ("Role", "Vai trò"), ("Notes", "Ghi chú")
    };
    WriteBilingualHeaders(ws, 2, accountHeaders);
    StyleHeader(ws.Range(2, 1, 2, accountHeaders.Length));

    var accounts = new object?[][]
    {
        new object?[] { 1, "user1@auctionhouse.local", "User@123", "User", Bilingual.Bi("Active; smoke default", "Active; mặc định smoke") },
        new object?[] { 2, "user3@auctionhouse.local", "User@123", "User", Bilingual.Bi("Active; PayPal checkout tests", "Active; test checkout PayPal") },
        new object?[] { 3, "user4@auctionhouse.local", "User@123", "User", Bilingual.Bi("Inactive — login rejected", "Inactive — login bị từ chối") },
        new object?[] { 4, "user12@auctionhouse.local", "User@123", "Admin", Bilingual.Bi("Use /Admin/Account/Login", "Dùng /Admin/Account/Login") },
        new object?[] { 5, "admin@auctionhouse.com", "User@123", "Admin", Bilingual.Bi("Full permissions superuser", "Superuser đủ quyền") },
    };
    WriteRows(ws, 3, accounts);

    var listingStart = 3 + accounts.Length + 2;
    ws.Cell(listingStart, 1).Value = Bilingual.Bi("=== Listing data patterns ===", "=== Mẫu dữ liệu listing ===");
    var listingHeaders = new (string En, string Vi)[]
    {
        ("No", "STT"), ("ListingType", "Loại listing"), ("ProductName", "Tên SP"), ("Category", "Danh mục"),
        ("Year", "Năm"), ("Grade", "Grade"), ("StartingPrice", "Giá khởi điểm"), ("BidStep", "Bước giá"),
        ("BuyNowPrice", "Giá Buy Now"), ("Price", "Giá"), ("Status", "Trạng thái"), ("Seller", "Seller"),
        ("RegStart", "Bắt đầu ĐK"), ("LiveStart", "Bắt đầu live"), ("LiveEnd", "Kết thúc live"),
        ("PrimaryImage", "Ảnh chính"), ("Docs", "Tài liệu")
    };
    WriteBilingualHeaders(ws, listingStart + 1, listingHeaders);
    StyleHeader(ws.Range(listingStart + 1, 1, listingStart + 1, listingHeaders.Length));

    var listings = new object?[][]
    {
        new object?[] { 1, "auction", "Charizard Holo PSA10", "Pokemon", 1999, "PSA 10", 500, 25, 600, null, "live", "user1", "T+1h", "T+7d", "T+7d+1h", "primary.jpg", 1 },
        new object?[] { 2, "buynow", "Pikachu Promo", "Pokemon", 2020, "PSA 9", null, null, null, 250, "live", "user1", "auto", "auto", "auto", "primary.jpg", 0 },
        new object?[] { 3, "auction", "Seller Submit Test", "Pokemon", 2022, "PSA 10", 100, 5, null, null, "confirming", "user1", "T+1h", "T+7d", "T+7d+1h", "primary.jpg", 0 },
        new object?[] { 4, "auction", "Multi-bidder test", "Pokemon", 2001, "BGS 9.5", 300, 10, null, null, "live", "user1", "past", "now", "T+2h", "primary.jpg", 0 },
        new object?[] { 5, "auction", "With PDF cert", "Pokemon", 2022, "PSA 10", 100, 5, null, null, "live", "user1", "T+1h", "T+7d", "T+7d+1h", "primary.jpg", 1 },
    };
    WriteRows(ws, listingStart + 2, listings);

    var noteRow = listingStart + 2 + listings.Length + 1;
    ws.Cell(noteRow, 1).Value = Bilingual.Bi("Notes:", "Ghi chú:");
    ws.Cell(noteRow + 1, 1).Value = Bilingual.Bi(
        "T+* = relative to now (AuctionScheduleHelper defaults)",
        "T+* = tương đối so với hiện tại (AuctionScheduleHelper mặc định)");
    ws.Cell(noteRow + 2, 1).Value = Bilingual.Bi(
        "PayPal: Sandbox Personal buyer account from developer.paypal.com",
        "PayPal: tài khoản buyer Sandbox Personal từ developer.paypal.com");
    ws.Cell(noteRow + 3, 1).Value = Bilingual.Bi(
        "Smoke: appsettings.Local.json → SmokeTesting.Enabled = true",
        "Smoke: appsettings.Local.json → SmokeTesting.Enabled = true");
    ws.Cell(noteRow + 4, 1).Value = Bilingual.Bi(
        "Base URL: http://localhost:5006",
        "Base URL: http://localhost:5006");

    ws.Columns().AdjustToContents();
}

static void WriteRows(IXLWorksheet ws, int startRow, object?[][] rows)
{
    for (var r = 0; r < rows.Length; r++)
    for (var c = 0; c < rows[r].Length; c++)
        ws.Cell(startRow + r, c + 1).Value = rows[r][c]?.ToString() ?? string.Empty;
}

static void BuildAutomatedTestsSheet(XLWorkbook workbook, TestRunSummary? testSummary)
{
    var ws = workbook.Worksheets.Add("automated tests");

    WriteBilingualHeaders(ws, 1, new (string En, string Vi)[]
    {
        ("Test class", "Class test"),
        ("Module", "Module"),
        ("Test count (approx)", "Số test (xấp xỉ)"),
        ("Maps to Type IDs", "Map tới mã Loại"),
        ("Result (OK/NG)", "Kết quả (OK/NG)"),
        ("Passed/Total", "Pass/Tổng"),
    });
    StyleHeader(ws.Range(1, 1, 1, 6));

    var rows = new (string Class, string ModuleEn, string ModuleVi, string Count, string Maps)[]
    {
        ("AdminAuctionFormSyncTests", "Admin listing forms", "Form listing Admin", "8", "ADM-SYNC-01..08"),
        ("AuctionVisibilityTests", "Catalog visibility", "Hiển thị catalog", "6", "CAT-01..05"),
        ("BidIncrementValidationTests", "Bid increment", "Bước giá bid", "theory", "BID-04"),
        ("BidRateLimitServiceTests", "Rate limit", "Rate limit", "3", "FRAUD-01, FRAUD-04"),
        ("BidServicePlaceBidTests", "Place bid", "Đặt giá", "15+", "BID-02..10"),
        ("ConfirmingStatusTests", "Confirming gates", "Chặn confirming", "10", "CONF-01..06, DOC-02"),
        ("DashboardFilterValidatorTests", "Dashboard filter", "Lọc dashboard", "4", "DASH-05"),
        ("ListingFeeCalculatorTests", "Listing fee", "Phí listing", "4", "FEE-04"),
        ("MarketplaceFeeCalculatorTests", "Marketplace fees", "Phí marketplace", "6", "FEE-01..03"),
        ("OrderCheckoutSelectionTests", "Order checkout", "Checkout order", "5", "ORD-02..03"),
        ("OrderPayPathFeeTests", "COD + fees", "COD + phí", "1", "ORD-05, FEE-03"),
        ("PayPalCaptureFlowTests", "PayPal + deposit", "PayPal + cọc", "10", "PAY-01..07, AUCTION_REG-04"),
        ("ProductDetailCanBidTests", "CanBid UI logic", "Logic CanBid UI", "5", "BID-11"),
        ("ProductDocumentDownloadTests", "Document auth", "Auth tài liệu", "5+", "DOC-01..04"),
        ("WinnerNonPaymentBidSelectorTests", "Runner-up selection", "Chọn á quân", "3", "WNP-01"),
        ("WinnerNonPaymentRecoveryIntegrationTests", "Non-payment recovery", "Recovery không trả", "5", "WNP-01..04"),
    };

    for (var i = 0; i < rows.Length; i++)
    {
        var r = i + 2;
        ws.Cell(r, 1).Value = rows[i].Class;
        ws.Cell(r, 2).Value = Bilingual.Bi(rows[i].ModuleEn, rows[i].ModuleVi);
        ws.Cell(r, 3).Value = rows[i].Count;
        ws.Cell(r, 4).Value = rows[i].Maps;

        if (testSummary != null && TestResultsCollector.TryGetClassResult(testSummary, rows[i].Class, out var cls))
        {
            var total = cls.Passed + cls.Failed + cls.Skipped;
            ws.Cell(r, 5).Value = cls.Success ? "OK" : "NG";
            ws.Cell(r, 6).Value = $"{cls.Passed}/{total}";
        }
    }

    if (testSummary != null)
    {
        ws.Cell(rows.Length + 3, 1).Value = Bilingual.Bi(
            $"Run: dotnet test — {testSummary.Passed}/{testSummary.Total} passed at {testSummary.RunAtUtc.ToLocalTime():yyyy-MM-dd HH:mm}",
            $"Chạy: dotnet test — {testSummary.Passed}/{testSummary.Total} pass lúc {testSummary.RunAtUtc.ToLocalTime():yyyy-MM-dd HH:mm}");
    }
    else
    {
        ws.Cell(rows.Length + 3, 1).Value = Bilingual.Bi(
            "Run: dotnet test OnlineAuction.Tests",
            "Chạy: dotnet test OnlineAuction.Tests");
    }

    ws.Cell(rows.Length + 4, 1).Value = Bilingual.Bi(
        "Regenerate with live results: dotnet run (default runs tests)",
        "Tạo lại với kết quả live: dotnet run (mặc định chạy test)");
    ws.Columns().AdjustToContents();
}

static void BuildRemarksSheet(XLWorkbook workbook)
{
    var ws = workbook.Worksheets.Add("Sheet1");

    ws.Cell(1, 1).Value = "No";
    ws.Cell(1, 2).Value = Bilingual.Bi("Content to fix", "Nội dung cần sửa");
    StyleHeader(ws.Range(1, 1, 1, 2));

    ws.Column(1).Width = 6;
    ws.Column(2).Width = 90;
    ws.SheetView.FreezeRows(1);
}
