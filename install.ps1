# Deskout Installer Script for Windows
# This script builds, publishes, and installs Deskout to the local user's AppData Programs directory.
# It sets up Start Menu shortcuts and Registry startup keys. No admin privileges required!

$ErrorActionPreference = "Stop"

# Check for Administrator privileges
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "This installer requires Administrator privileges to write to C:\Program Files." -ForegroundColor Yellow
    Write-Host "Restarting script with elevated privileges..." -ForegroundColor Yellow
    $psExe = (Get-Process -Id $PID).Path
    Start-Process $psExe -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
    Exit
}

Set-Location $PSScriptRoot

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "       DESKOUT INSTALLER FOR WINDOWS      " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# 1. Paths configuration
$InstallDir = "C:\Program Files\Deskout"
$ShortcutPath = "C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Deskout.lnk"
$PublishDir = Join-Path $PSScriptRoot "bin\Release\net8.0-windows\win-x64\publish"

# 2. Build and Publish
Write-Host "[1/5] Building and publishing application..." -ForegroundColor Green
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=false --self-contained false

# 3. Create Install Directory and Copy Files
Write-Host "[2/5] Installing files to $InstallDir..." -ForegroundColor Green
if (Test-Path $InstallDir) {
    # If app is running, try to stop it first
    Stop-Process -Name "Deskout" -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    Remove-Item -Path $InstallDir -Recurse -Force
}
# Clean up legacy user-level AppData installation if present
$OldInstallDir = Join-Path $env:LocalAppData "Programs\Deskout"
if (Test-Path $OldInstallDir) {
    Remove-Item -Path $OldInstallDir -Recurse -Force -ErrorAction SilentlyContinue
}
$OldShortcutPath = Join-Path $env:AppData "Microsoft\Windows\Start Menu\Programs\Deskout.lnk"
if (Test-Path $OldShortcutPath) {
    Remove-Item -Path $OldShortcutPath -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
Copy-Item -Path (Join-Path $PublishDir "*") -Destination $InstallDir -Recurse -Force

# 4. Create Start Menu Shortcut
Write-Host "[3/5] Creating Start Menu shortcut..." -ForegroundColor Green
$WshShell = New-Object -ComObject WScript.Shell
$Shortcut = $WshShell.CreateShortcut($ShortcutPath)
$Shortcut.TargetPath = Join-Path $InstallDir "Deskout.exe"
$Shortcut.WorkingDirectory = $InstallDir
$Shortcut.Description = "Deskout Windows Shutdown Task Reminder"
$Shortcut.Save()

# 5. Add Registry Run key for Automatic Windows Startup
Write-Host "[4/5] Configuring startup registry key..." -ForegroundColor Green
$RegistryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
$ExePath = [char]34 + (Join-Path $InstallDir "Deskout.exe") + [char]34 + " --background"
Set-ItemProperty -Path $RegistryPath -Name "Deskout" -Value $ExePath -Force | Out-Null

# 6. Launch Application in Background
Write-Host "[5/6] Starting Deskout in background..." -ForegroundColor Green
Start-Process -FilePath (Join-Path $InstallDir "Deskout.exe") -ArgumentList "--background"

# 7. Compile Setup Installer with Inno Setup
Write-Host "[6/6] Compiling sharing installer EXE..." -ForegroundColor Green
$IsccExe = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
if (-not (Test-Path $IsccExe)) {
    $IsccExe = Join-Path $env:LocalAppData "Programs\Inno Setup 6\ISCC.exe"
}
$SetupPath = Join-Path $PSScriptRoot "publish_setup\DeskoutSetup.exe"
if (Test-Path $IsccExe) {
    & $IsccExe (Join-Path $PSScriptRoot "setup.iss") | Out-Null
} else {
    Write-Host "Inno Setup compiler not found. Skipping Setup Installer build." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "      INSTALLATION COMPLETED SUCCESS!    " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Deskout is now installed and running in the background." -ForegroundColor White
Write-Host "You can find it in your system tray (bottom-right taskbar)." -ForegroundColor White
Write-Host "A Start Menu shortcut was created at:" -ForegroundColor White
Write-Host "   $ShortcutPath" -ForegroundColor DarkGray
Write-Host "The application is registered to start with Windows." -ForegroundColor White
if (Test-Path $SetupPath) {
    Write-Host ""
    Write-Host "A redistributable setup installer was created successfully at:" -ForegroundColor Green
    Write-Host "   $SetupPath" -ForegroundColor Green
}
Write-Host ""
