# Builds the signed EasySchool APK and puts it where the web site serves it:
#   School.Web/AppDownloads/EasySchool.apk  ->  https://<site>/EasySchool/app
# Publish School.Web afterwards (the folder goes with it), then raise
# App:LatestVersion in School.API/appsettings.json to the version printed below
# so every phone offers the update.
#
#   powershell -ExecutionPolicy Bypass -File build-apk.ps1
#
# Needs android/key.properties + android/keys/easyschool-release.jks (the
# release key - NOT in git; keep a copy safe). Without them the APK is signed
# with the debug key and will not install over the parents' copy.
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$env:Path = "$env:USERPROFILE\dev\flutter\bin;" + $env:Path
if (-not $env:JAVA_HOME) { $env:JAVA_HOME = 'C:\Program Files\Microsoft\jdk-17.0.20.101-hotspot' }

if (-not (Test-Path "$here\android\key.properties")) {
    throw 'android\key.properties is missing - restore the release key first (see the comment at the top).'
}

Push-Location $here
try {
    # flutter prints warnings on stderr; PowerShell 5.1 would treat those as errors.
    $ErrorActionPreference = 'Continue'
    flutter build apk --release --target-platform android-arm,android-arm64 --obfuscate --split-debug-info=build/symbols
    if ($LASTEXITCODE -ne 0) { throw 'flutter build failed' }
    $ErrorActionPreference = 'Stop'
} finally { Pop-Location }

$apk = "$here\build\app\outputs\flutter-apk\app-release.apk"
$dest = Join-Path (Resolve-Path "$here\..\..\School.Web") 'AppDownloads'
New-Item -ItemType Directory -Force $dest | Out-Null
Copy-Item $apk "$dest\EasySchool.apk" -Force

$version = (Select-String -Path "$here\pubspec.yaml" -Pattern '^version:\s*([0-9.]+)').Matches[0].Groups[1].Value
$mb = [math]::Round((Get-Item "$dest\EasySchool.apk").Length / 1MB, 1)
Write-Host ""
Write-Host "EasySchool $version ($mb MB) -> $dest\EasySchool.apk" -ForegroundColor Green
Write-Host "Next: publish School.Web, then set App:LatestVersion = `"$version`" in School.API appsettings.json."
