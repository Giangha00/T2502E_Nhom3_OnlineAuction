record ManualVerificationResult(
    string ActualEn,
    string ActualVi,
    string NotesEn,
    string NotesVi);

static class ManualVerificationMap
{
    /// <summary>Spec cases verified manually in browser / HTTP (curl) on local dev.</summary>
    public static readonly IReadOnlyDictionary<string, ManualVerificationResult> Verified = new Dictionary<string, ManualVerificationResult>(StringComparer.Ordinal)
    {
        ["AUTH-01"] = new(
            "SignUp HTTP 302; POST /Smoke/ConfirmEmail → success:true",
            "SignUp HTTP 302; POST /Smoke/ConfirmEmail → success:true",
            "Verified localhost:5006; SmokeTesting.Enabled=true",
            "Đã kiểm tra localhost:5006; SmokeTesting.Enabled=true"),
        ["AUTH-02"] = new(
            "Duplicate user1@auctionhouse.local → \"Email already exists\"",
            "Trùng user1@auctionhouse.local → \"Email already exists\"",
            "POST /Auth/SignUp; no duplicate user created",
            "POST /Auth/SignUp; không tạo user trùng"),
        ["AUTH-03"] = new(
            "Login user1@auctionhouse.local / User@123 → .AuctionHouse.User cookie set",
            "Login user1@auctionhouse.local / User@123 → cookie .AuctionHouse.User được set",
            "POST /Auth/Login fromModal=true",
            "POST /Auth/Login fromModal=true"),
        ["ADM-AUTH-03"] = new(
            "User logged in → GET /Admin/Dashboard → 302 to /Admin/Account/Login",
            "User đã login → GET /Admin/Dashboard → 302 tới /Admin/Account/Login",
            "Public User cookie cannot access Admin area",
            "Cookie User public không truy cập được Admin"),
    };
}
