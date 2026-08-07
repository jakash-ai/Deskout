# Deskout Installer Script for Windows
# This script builds, publishes, and installs Deskout to the local user's AppData Programs directory.
# It sets up Start Menu shortcuts and Registry startup keys. No admin privileges required!

$ErrorActionPreference = "Stop"

Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "       DESKOUT INSTALLER FOR WINDOWS      " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan

# 1. Paths configuration
$InstallDir = Join-Path $env:LocalAppData "Programs\Deskout"
$ShortcutPath = Join-Path $env:AppData "Microsoft\Windows\Start Menu\Programs\Deskout.lnk"
$PublishDir = Join-Path $PSScriptRoot "bin\Release\net8.0-windows\win-x64\publish"

# 2. Build and Publish
Write-Host "[1/5] Building and publishing application..." -ForegroundColor Green
dotnet publish -c Release -r win-x64 --self-contained false

# 3. Create Install Directory and Copy Files
Write-Host "[2/5] Installing files to $InstallDir..." -ForegroundColor Green
if (Test-Path $InstallDir) {
    # If app is running, try to stop it first
    Stop-Process -Name "Deskout" -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    Remove-Item -Path $InstallDir -Recurse -Force
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
Write-Host "[5/5] Starting Deskout in background..." -ForegroundColor Green
Start-Process -FilePath (Join-Path $InstallDir "Deskout.exe") -ArgumentList "--background"

Write-Host ""
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "      INSTALLATION COMPLETED SUCCESS!    " -ForegroundColor Cyan
Write-Host "=========================================" -ForegroundColor Cyan
Write-Host "Deskout is now installed and running in the background." -ForegroundColor White
Write-Host "You can find it in your system tray (bottom-right taskbar)." -ForegroundColor White
Write-Host "A Start Menu shortcut was created at:" -ForegroundColor White
Write-Host "   $ShortcutPath" -ForegroundColor DarkGray
Write-Host "The application is registered to start with Windows." -ForegroundColor White
Write-Host ""
