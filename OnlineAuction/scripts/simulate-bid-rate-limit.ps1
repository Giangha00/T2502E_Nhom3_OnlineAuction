# Simulates exceeding bid rate limit against a running OnlineAuction instance.
# Expects authenticated cookie + antiforgery token (bot with cookie still hits 429).
#
# Usage:
#   .\simulate-bid-rate-limit.ps1 -BaseUrl "https://localhost:7xxx" -AuctionId 1 -Cookie ".AuctionHouse.User=..." -AntiforgeryToken "..."
#   Optional: -ChallengeToken "stub-ok" -Limit 12

param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl,

    [Parameter(Mandatory = $true)]
    [int]$AuctionId,

    [Parameter(Mandatory = $true)]
    [string]$Cookie,

    [Parameter(Mandatory = $true)]
    [string]$AntiforgeryToken,

    [decimal]$Amount = 1000,

    [int]$Limit = 12,

    [string]$ChallengeToken = "",

    [decimal]$BidStep = 10
)

$ErrorActionPreference = "Stop"
$uri = [Uri]$BaseUrl.TrimEnd("/")
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession

$cookieParts = $Cookie -split ";", 2
$nameValue = $cookieParts[0].Trim()
$eq = $nameValue.IndexOf("=")
if ($eq -lt 1) {
    throw "Cookie must look like '.AuctionHouse.User=VALUE'"
}

$cookieName = $nameValue.Substring(0, $eq)
$cookieValue = $nameValue.Substring($eq + 1)
$session.Cookies.Add((New-Object System.Net.Cookie($cookieName, $cookieValue, "/", $uri.Host)))

Write-Host "Sending $Limit bid attempts to $($uri.AbsoluteUri)Auction/PlaceBid (auction=$AuctionId)..."

$blocked = $false
for ($i = 1; $i -le $Limit; $i++) {
    $bidAmount = $Amount + (($i - 1) * $BidStep)
    $body = @{
        auctionId                  = $AuctionId
        amount                     = $bidAmount
        __RequestVerificationToken = $AntiforgeryToken
        challengeToken             = $ChallengeToken
    }

    $headers = @{
        "RequestVerificationToken" = $AntiforgeryToken
        "X-Requested-With"         = "XMLHttpRequest"
    }
    if ($ChallengeToken) {
        $headers["X-Bid-Challenge-Token"] = $ChallengeToken
    }

    try {
        $response = Invoke-WebRequest `
            -Uri ($uri.AbsoluteUri.TrimEnd("/") + "/Auction/PlaceBid") `
            -Method POST `
            -WebSession $session `
            -Headers $headers `
            -Body $body `
            -ContentType "application/x-www-form-urlencoded"

        $status = [int]$response.StatusCode
        Write-Host ("[{0}] HTTP {1} body={2}" -f $i, $status, $response.Content)
    }
    catch {
        $status = $null
        if ($_.Exception.Response) {
            $status = [int]$_.Exception.Response.StatusCode
        }

        Write-Host ("[{0}] HTTP {1} ({2})" -f $i, $status, $_.Exception.Message)

        if ($status -eq 429) {
            $blocked = $true
            Write-Host "Hard rate limit hit (429). Bid was not inserted." -ForegroundColor Yellow
            break
        }

        if ($null -eq $status) {
            break
        }
    }

    Start-Sleep -Milliseconds 50
}

if (-not $blocked) {
    Write-Host "Did not observe HTTP 429. Lower MaxBidsPerMinutePerUser or increase -Limit." -ForegroundColor Red
    exit 1
}

Write-Host "Simulation OK: rate limit blocked further bids." -ForegroundColor Green
exit 0
