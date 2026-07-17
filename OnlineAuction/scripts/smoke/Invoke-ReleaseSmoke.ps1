# Release smoke pack (<= 20 min): AUTH-REG-01 → AUTH-LOGIN-01 → AUCTION_REG-03 → BID-01
#
# Prerequisites:
#   1. App running (Development):  dotnet run --launch-profile http
#   2. appsettings.Local.json:     "SmokeTesting": { "Enabled": true }
#   3. DB seeded with at least one registerable live auction
#
# Usage:
#   cd OnlineAuction
#   .\scripts\smoke\Invoke-ReleaseSmoke.ps1
#   .\scripts\smoke\Invoke-ReleaseSmoke.ps1 -BaseUrl "http://localhost:5006" -AuctionId 12
#   .\scripts\smoke\Invoke-ReleaseSmoke.ps1 -OpenBugs "BUG-12: deposit timeout (open)"
#
# Exit codes: 0 = all pass, 1 = smoke fail (block related release)

param(
    [string]$BaseUrl = "http://localhost:5006",
    [int]$AuctionId = 0,
    [string]$Password = "Smoke@12345",
    [string]$ChallengeToken = "stub-ok",
    [string]$OpenBugs = "",
    [string]$ReportPath = "",
    [string]$EnvironmentName = "local-dev",
    [switch]$SkipSignup
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$startedAt = Get-Date
$cases = [System.Collections.Generic.List[object]]::new()
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).Path
$onlineAuctionRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path

function Get-GitCommit {
    try {
        Push-Location $repoRoot
        return (git rev-parse --short HEAD 2>$null)
    }
    catch {
        return "unknown"
    }
    finally {
        Pop-Location
    }
}

function Get-BuildInfo {
    $dll = Join-Path $onlineAuctionRoot "bin\Debug\net8.0\OnlineAuction.dll"
    if (Test-Path $dll) {
        return (Get-Item $dll).LastWriteTimeUtc.ToString("yyyy-MM-ddTHH:mm:ssZ")
    }
    return "unknown"
}

function Add-CaseResult {
    param(
        [string]$CaseId,
        [string]$Name,
        [ValidateSet("PASS", "FAIL", "SKIP")]
        [string]$Result,
        [string]$Detail
    )
    $cases.Add([pscustomobject]@{
            CaseId = $CaseId
            Name   = $Name
            Result = $Result
            Detail = $Detail
        }) | Out-Null

    $color = switch ($Result) {
        "PASS" { "Green" }
        "SKIP" { "Yellow" }
        default { "Red" }
    }
    Write-Host ("[{0}] {1} — {2}" -f $CaseId, $Result, $Detail) -ForegroundColor $color
}

function Get-AntiForgeryToken {
    param($Session, [string]$Url)
    $page = Invoke-WebRequest -Uri $Url -WebSession $Session -UseBasicParsing
    if ($page.Content -match 'name="request-verification-token"\s+content="([^"]+)"') {
        return $Matches[1]
    }
    if ($page.Content -match 'name="__RequestVerificationToken"\s+type="hidden"\s+value="([^"]+)"') {
        return $Matches[1]
    }
    throw "Antiforgery token not found at $Url"
}

function Invoke-FormPost {
    param(
        $Session,
        [string]$Url,
        [hashtable]$Fields,
        [hashtable]$Headers = @{},
        [switch]$AllowError
    )

    $body = @{ }
    foreach ($key in $Fields.Keys) {
        $body[$key] = $Fields[$key]
    }

    $mergedHeaders = @{
        "X-Requested-With" = "XMLHttpRequest"
    }
    foreach ($key in $Headers.Keys) {
        $mergedHeaders[$key] = $Headers[$key]
    }

    try {
        return Invoke-WebRequest `
            -Uri $Url `
            -Method POST `
            -WebSession $Session `
            -Body $body `
            -ContentType "application/x-www-form-urlencoded" `
            -Headers $mergedHeaders `
            -MaximumRedirection 5 `
            -UseBasicParsing
    }
    catch {
        if ($AllowError -and $_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $content = $reader.ReadToEnd()
            return [pscustomobject]@{
                StatusCode = [int]$_.Exception.Response.StatusCode
                Content    = $content
            }
        }
        throw
    }
}

$base = $BaseUrl.TrimEnd("/")
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
$stamp = Get-Date -Format "yyyyMMddHHmmss"
$email = "smoke.$stamp@auctionhouse.local"
$phone = ("09{0}" -f (Get-Random -Minimum 100000000 -Maximum 999999999)).Substring(0, 11)
$fullName = "Smoke Bidder $stamp"
$commit = Get-GitCommit
$build = Get-BuildInfo

Write-Host "=== Release smoke pack ===" -ForegroundColor Cyan
Write-Host "BaseUrl=$base  Env=$EnvironmentName  Commit=$commit"
Write-Host ""

# ---------------------------------------------------------------------------
# AUTH-REG-01 — Sign up
# ---------------------------------------------------------------------------
try {
    if ($SkipSignup) {
        $email = "user1@auctionhouse.local"
        $Password = "User@123"
        Add-CaseResult -CaseId "AUTH-REG-01" -Name "Sign up + confirm" -Result "SKIP" -Detail "Skipped (-SkipSignup); using seed user $email — not valid alone for release DoD"
    }
    else {
        $token = Get-AntiForgeryToken -Session $session -Url "$base/"
        $signup = Invoke-FormPost -Session $session -Url "$base/Auth/SignUp" -Fields @{
            FullName                   = $fullName
            Email                      = $email
            PhoneNumber                = $phone
            Password                   = $Password
            ConfirmPassword            = $Password
            __RequestVerificationToken = $token
            fromModal                  = "true"
        } -AllowError

        $signupOk = $signup.StatusCode -ge 200 -and $signup.StatusCode -lt 400
        if (-not $signupOk) {
            $snippet = if ($signup.Content) { $signup.Content.Substring(0, [Math]::Min(200, $signup.Content.Length)) } else { "" }
            throw "SignUp HTTP $($signup.StatusCode): $snippet"
        }

        $confirm = Invoke-FormPost -Session $session -Url "$base/Smoke/ConfirmEmail" -Fields @{
            email = $email
        } -AllowError

        if ($confirm.StatusCode -eq 404) {
            throw "Smoke/ConfirmEmail returned 404. Enable SmokeTesting in Development (appsettings.Local.json)."
        }

        $confirmJson = $confirm.Content | ConvertFrom-Json
        if (-not $confirmJson.success) {
            throw "Smoke confirm failed: $($confirmJson.message)"
        }

        Add-CaseResult -CaseId "AUTH-REG-01" -Name "Sign up + confirm" -Result "PASS" -Detail "Created $email and confirmed via /Smoke/ConfirmEmail"
    }
}
catch {
    Add-CaseResult -CaseId "AUTH-REG-01" -Name "Sign up + confirm" -Result "FAIL" -Detail $_.Exception.Message
}

# ---------------------------------------------------------------------------
# AUTH-LOGIN-01 — Login
# ---------------------------------------------------------------------------
try {
    $token = Get-AntiForgeryToken -Session $session -Url "$base/"
    $login = Invoke-FormPost -Session $session -Url "$base/Auth/Login" -Fields @{
        Email                      = $email
        Password                   = $Password
        RememberMe                 = "false"
        __RequestVerificationToken = $token
        fromModal                  = "true"
    } -AllowError

    $hasUserCookie = $false
    foreach ($cookie in $session.Cookies.GetCookies([Uri]$base)) {
        if ($cookie.Name -eq ".AuctionHouse.User") {
            $hasUserCookie = $true
            break
        }
    }

    $loginOk = $login.StatusCode -ge 200 -and $login.StatusCode -lt 400 -and $hasUserCookie
    if (-not $loginOk) {
        $names = ($session.Cookies.GetCookies([Uri]$base) | ForEach-Object { $_.Name }) -join ", "
        throw "Login HTTP $($login.StatusCode); cookies=[$names]"
    }

    Add-CaseResult -CaseId "AUTH-LOGIN-01" -Name "Login" -Result "PASS" -Detail "Logged in as $email"
}
catch {
    Add-CaseResult -CaseId "AUTH-LOGIN-01" -Name "Login" -Result "FAIL" -Detail $_.Exception.Message
}

# ---------------------------------------------------------------------------
# Resolve auction
# ---------------------------------------------------------------------------
if ($AuctionId -le 0) {
    try {
        $pick = Invoke-WebRequest -Uri "$base/Smoke/PickAuction" -WebSession $session -UseBasicParsing
        $pickJson = $pick.Content | ConvertFrom-Json
        $AuctionId = [int]$pickJson.auction.Id
        Write-Host "Picked auction #$AuctionId ($($pickJson.auction.productName))" -ForegroundColor DarkGray
    }
    catch {
        Write-Host "WARN: could not auto-pick auction: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

# ---------------------------------------------------------------------------
# AUCTION_REG-03 — Register + deposit
# ---------------------------------------------------------------------------
try {
    if ($AuctionId -le 0) {
        throw "No AuctionId. Pass -AuctionId or enable SmokeTesting PickAuction."
    }

    $token = Get-AntiForgeryToken -Session $session -Url "$base/Auction/Detail/$AuctionId"
    $depositMode = "SmokeBypass"

    # Prefer real InitiateDeposit when PayPal is configured; still complete via smoke if needed.
    try {
        $initiate = Invoke-FormPost -Session $session -Url "$base/Auction/InitiateDeposit" -Fields @{
            auctionId                  = $AuctionId
            __RequestVerificationToken = $token
        } -Headers @{ "RequestVerificationToken" = $token } -AllowError

        if ($initiate.StatusCode -ge 200 -and $initiate.StatusCode -lt 300) {
            $initJson = $initiate.Content | ConvertFrom-Json
            if ($initJson.success -and $initJson.approvalUrl) {
                $depositMode = "PayPalInitiated+SmokeCapture"
            }
        }
    }
    catch {
        # PayPal optional for smoke; bypass path below is authoritative for gate.
    }

    $complete = Invoke-FormPost -Session $session -Url "$base/Smoke/CompleteRegistrationDeposit" -Fields @{
        auctionId = $AuctionId
    } -AllowError

    if ($complete.StatusCode -eq 404) {
        throw "Smoke/CompleteRegistrationDeposit 404. Enable SmokeTesting:Enabled in Development."
    }

    $completeJson = $complete.Content | ConvertFrom-Json
    if (-not $completeJson.success) {
        throw $completeJson.message
    }

    Add-CaseResult -CaseId "AUCTION_REG-03" -Name "Register + deposit" -Result "PASS" -Detail "Auction #$AuctionId mode=$depositMode deposit=$($completeJson.depositAmount)"
}
catch {
    Add-CaseResult -CaseId "AUCTION_REG-03" -Name "Register + deposit" -Result "FAIL" -Detail $_.Exception.Message
}

# ---------------------------------------------------------------------------
# BID-01 — Place bid
# ---------------------------------------------------------------------------
try {
    if ($AuctionId -le 0) {
        throw "No AuctionId for bid."
    }

    $state = Invoke-WebRequest -Uri "$base/Auction/BidState/$AuctionId" -WebSession $session -UseBasicParsing
    $stateJson = $state.Content | ConvertFrom-Json
    if ($stateJson.isEnded) {
        throw "Auction #$AuctionId already ended."
    }

    $amount = [decimal]$stateJson.minNextBid
    $token = Get-AntiForgeryToken -Session $session -Url "$base/Auction/Detail/$AuctionId"
    $bid = Invoke-FormPost -Session $session -Url "$base/Auction/PlaceBid" -Fields @{
        auctionId                  = $AuctionId
        amount                     = $amount
        challengeToken             = $ChallengeToken
        __RequestVerificationToken = $token
    } -Headers @{
        "RequestVerificationToken" = $token
        "X-Bid-Challenge-Token"    = $ChallengeToken
    } -AllowError

    $bidJson = $null
    try { $bidJson = $bid.Content | ConvertFrom-Json } catch { }

    if ($bid.StatusCode -eq 403 -and $bidJson -and $bidJson.requiresChallenge) {
        $bid = Invoke-FormPost -Session $session -Url "$base/Auction/PlaceBid" -Fields @{
            auctionId                  = $AuctionId
            amount                     = $amount
            challengeToken             = $ChallengeToken
            __RequestVerificationToken = $token
        } -Headers @{
            "RequestVerificationToken" = $token
            "X-Bid-Challenge-Token"    = $ChallengeToken
        } -AllowError
        try { $bidJson = $bid.Content | ConvertFrom-Json } catch { }
    }

    if ($bid.StatusCode -lt 200 -or $bid.StatusCode -ge 300 -or -not $bidJson -or -not $bidJson.success) {
        $msg = if ($bidJson) { $bidJson.message } else { $bid.Content }
        throw "PlaceBid HTTP $($bid.StatusCode): $msg"
    }

    Add-CaseResult -CaseId "BID-01" -Name "Place bid" -Result "PASS" -Detail "Bid $amount on auction #$AuctionId (price=$($bidJson.currentPrice))"
}
catch {
    Add-CaseResult -CaseId "BID-01" -Name "Place bid" -Result "FAIL" -Detail $_.Exception.Message
}

# ---------------------------------------------------------------------------
# Report
# ---------------------------------------------------------------------------
$endedAt = Get-Date
$durationMin = [Math]::Round(($endedAt - $startedAt).TotalMinutes, 2)
$passed = @($cases | Where-Object { $_.Result -eq "PASS" }).Count
$failed = @($cases | Where-Object { $_.Result -eq "FAIL" }).Count
$skipped = @($cases | Where-Object { $_.Result -eq "SKIP" }).Count
$total = $cases.Count
$passRate = if ($total -gt 0) { [Math]::Round(100.0 * $passed / $total, 1) } else { 0 }
$allPassed = ($failed -eq 0 -and $skipped -eq 0 -and $passed -eq $total -and $total -gt 0)
$gate = if ($allPassed) {
    "RELEASE GATE: OPEN (smoke pass)"
}
elseif ($failed -gt 0) {
    "RELEASE GATE: BLOCKED (smoke fail → do not release related feature)"
}
else {
    "RELEASE GATE: BLOCKED (incomplete smoke — remove -SkipSignup for release DoD)"
}

if (-not $ReportPath) {
    $reportDir = Join-Path $PSScriptRoot "reports"
    New-Item -ItemType Directory -Force -Path $reportDir | Out-Null
    $ReportPath = Join-Path $reportDir ("smoke-report-{0}.md" -f (Get-Date -Format "yyyyMMdd-HHmmss"))
}

$caseLines = ($cases | ForEach-Object {
        "| $($_.CaseId) | $($_.Name) | $($_.Result) | $($_.Detail) |"
    }) -join "`n"

$openBugsText = if ([string]::IsNullOrWhiteSpace($OpenBugs)) { "_None logged for this run._" } else { $OpenBugs }

$report = @"
# Release smoke report

| Field | Value |
|-------|-------|
| Date (local) | $($endedAt.ToString("yyyy-MM-dd HH:mm")) |
| Environment | $EnvironmentName |
| Base URL | $base |
| Build (dll UTC) | $build |
| Git commit | `$commit` |
| Duration | $durationMin min (budget ≤ 20) |
| Pass rate | **$passed / $total ($passRate%)** |
| Gate | **$gate** |

## Cases

| ID | Name | Result | Detail |
|----|------|--------|--------|
$caseLines

## Open bugs

$openBugsText

## Definition

Smoke fail → **block release** of Auth / Auction registration-deposit / Bid features touched by this pack.

## Notes

- AUTH-REG-01 uses `/Smoke/ConfirmEmail` when Gmail is unavailable (requires ``SmokeTesting:Enabled`` + Development).
- AUCTION_REG-03 uses `/Smoke/CompleteRegistrationDeposit` to finish deposit without interactive PayPal (optional InitiateDeposit first).
- BID-01 sends challenge token ``$ChallengeToken`` when fraud challenge is enabled.
"@

Set-Content -Path $ReportPath -Value $report -Encoding UTF8

Write-Host ""
Write-Host $gate -ForegroundColor $(if ($allPassed) { "Green" } else { "Red" })
Write-Host "Pass rate: $passed/$total ($passRate%) in $durationMin min"
Write-Host "Report: $ReportPath"

if (-not $allPassed) {
    exit 1
}
exit 0
