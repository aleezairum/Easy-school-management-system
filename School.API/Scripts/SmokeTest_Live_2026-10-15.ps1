<#
=============================================================================
 Smoke test after publishing the 2026-10-15 release (JWT on the API, mobile
 app, app chat, download page). Takes under a minute; changes nothing.

   powershell -ExecutionPolicy Bypass -File SmokeTest_Live_2026-10-15.ps1

 Every line should say OK. If the web portal misbehaves for the office:
   School.API\appsettings.json -> "Jwt": { "RequireForWebApi": false }
   then recycle the API app pool. The portal works exactly as before.

 This file is plain ASCII on purpose (Windows PowerShell 5.1).
=============================================================================
#>
param(
    [string]$ApiBase = "https://school.easypos.com.pk/api",
    [string]$WebBase = "https://school.easypos.com.pk/EasySchool",
    # Optional: an office login. Then the test also signs in on the web and
    # proves the web's token opens the API (same Jwt:Key in both sites).
    [string]$User,
    [string]$Password
)

$ErrorActionPreference = "Continue"
$fails = 0

function Get-Status([string]$Url, [string]$Method = "GET", [string]$Body = $null) {
    try {
        $p = @{ Uri = $Url; Method = $Method; UseBasicParsing = $true; TimeoutSec = 30; MaximumRedirection = 0 }
        if ($Body) { $p.Body = $Body; $p.ContentType = "application/json" }
        $r = Invoke-WebRequest @p
        return @{ Code = [int]$r.StatusCode; Text = $r.Content; Headers = $r.Headers }
    } catch {
        $resp = $_.Exception.Response
        if ($resp) { return @{ Code = [int]$resp.StatusCode; Text = ""; Headers = @{} } }
        return @{ Code = 0; Text = $_.Exception.Message; Headers = @{} }
    }
}

function Check([string]$Name, [bool]$Pass, [string]$Detail) {
    if ($Pass) { Write-Host ("  OK    " + $Name) -ForegroundColor Green }
    else { Write-Host ("  FAIL  " + $Name + "   " + $Detail) -ForegroundColor Red; $script:fails++ }
}

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

Write-Host ""
Write-Host "=== API ===" -ForegroundColor Cyan
$r = Get-Status "$ApiBase/AcademicSessionYear"
Check "API is up (session list for the sign-in page)" ($r.Code -eq 200) "status $($r.Code) $($r.Text)"

$r = Get-Status "$ApiBase/Student"
Check "API refuses calls without a token (JWT is on)" ($r.Code -eq 401) "status $($r.Code) - expected 401"

$r = Get-Status "$ApiBase/SecUser/login" "POST" '{"UserLogin":"smoke-test","UserPassword":"x"}'
Check "Web sign-in endpoint answers (wrong password -> 401, not an error)" ($r.Code -eq 401) "status $($r.Code)"

$r = Get-Status "$ApiBase/app/school-info"
$ok = $false
if ($r.Code -eq 200) { $j = $r.Text | ConvertFrom-Json; $ok = ($j.schoolCode -eq "SSMHS" -and $j.latestVersion) }
Check "Mobile app API is published (school code SSMHS, latest version)" $ok "status $($r.Code) $($r.Text)"

$r = Get-Status "$ApiBase/Sms/webhook?hub.mode=subscribe&hub.verify_token=smoke&hub.challenge=1"
Check "Meta webhook still open (not 401)" ($r.Code -ne 401 -and $r.Code -ne 0) "status $($r.Code)"

$r = Get-Status "$ApiBase/hikvision/event/smoke-test" "POST" "{}"
Check "Attendance machines can still post punches (not 401)" ($r.Code -ne 401 -and $r.Code -ne 0) "status $($r.Code)"

Write-Host ""
Write-Host "=== Web ===" -ForegroundColor Cyan
$r = Get-Status "$WebBase/login"
Check "Web sign-in page" ($r.Code -eq 200) "status $($r.Code)"

$r = Get-Status "$WebBase/app"
Check "App download page (public)" ($r.Code -eq 200 -and $r.Text -match "Download for Android") "status $($r.Code)"

try {
    $h = Invoke-WebRequest "$WebBase/app/download" -Method Head -UseBasicParsing -TimeoutSec 60
    $len = [long]$h.Headers["Content-Length"]
    Check ("APK downloads (" + [math]::Round($len / 1MB, 1) + " MB)") ($h.Headers["Content-Type"] -match "android" -and $len -gt 20MB) "type $($h.Headers['Content-Type'])"
} catch { Check "APK downloads" $false $_.Exception.Message }

$r = Get-Status "$WebBase/app/qr.png"
Check "QR code for the login slips" ($r.Code -eq 200) "status $($r.Code)"

if ($User) {
    Write-Host ""
    Write-Host "=== Signed in as $User ===" -ForegroundColor Cyan
    try {
        $sessions = Invoke-RestMethod "$ApiBase/AcademicSessionYear" -TimeoutSec 30
        $current = @($sessions | Where-Object { $_.isCurrent }) + @($sessions) | Select-Object -First 1
        $web = New-Object Microsoft.PowerShell.Commands.WebRequestSession
        Invoke-WebRequest "$WebBase/login" -WebSession $web -UseBasicParsing -TimeoutSec 30 | Out-Null
        $form = @{ username = $User; password = $Password; session = $current.vid }
        Invoke-WebRequest "$WebBase/login" -Method Post -Body $form -WebSession $web -UseBasicParsing -TimeoutSec 30 | Out-Null
        $ping = Invoke-RestMethod "$WebBase/session/ping" -WebSession $web -TimeoutSec 30
        Check "Web sign-in works and hands the page an API token" ([bool]$ping.token) "no token - check Jwt:Key in School.Web appsettings.json"
        if ($ping.token) {
            $r = Invoke-WebRequest "$ApiBase/Student" -Headers @{ Authorization = "Bearer " + $ping.token } -UseBasicParsing -TimeoutSec 60
            Check "The web's token opens the API (Jwt:Key is the same in both sites)" ($r.StatusCode -eq 200) "status $($r.StatusCode)"
        }
    } catch {
        Check "Web sign-in and API token" $false ($_.Exception.Message + " - if 401: Jwt:Key differs between School.API and School.Web")
    }
}

Write-Host ""
if ($fails -eq 0) {
    Write-Host "All checks passed." -ForegroundColor Green
    Write-Host "Now sign in on the web and open: Dashboard, Mark Daily Attendance, Receive Payment, Students."
} else {
    Write-Host "$fails check(s) failed - see above." -ForegroundColor Red
    Write-Host "If the office cannot work: set Jwt:RequireForWebApi = false in School.API appsettings.json and recycle the API app pool."
}
