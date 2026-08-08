# Deskout Uninstaller Script for Windows
# Stops the application and cleans up all installed files, shortcuts, and registry keys.

$ErrorActionPreference = "Continue"

# Check for Administrator privileges
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "This uninstaller requires Administrator privileges to clean up C:\Program Files." -ForegroundColor Yellow
    Write-Host "Restarting script with elevated privileges..." -ForegroundColor Yellow
    $psExe = (Get-Process -Id $PID).Path
    Start-Process $psExe -ArgumentList "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`"" -Verb RunAs
    Exit
}

Set-Location $PSScriptRoot

Write-Host "=========================================" -ForegroundColor Red
Write-Host "      DESKOUT UNINSTALLER FOR WINDOWS     " -ForegroundColor Red
Write-Host "=========================================" -ForegroundColor Red

# 1. Stop the application if running
Write-Host "[1/4] Stopping Deskout processes..." -ForegroundColor Yellow
Stop-Process -Name "Deskout" -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 1

# 2. Remove Registry Run Key
Write-Host "[2/4] Removing startup registry entries..." -ForegroundColor Yellow
$RegistryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Run"
Remove-ItemProperty -Path $RegistryPath -Name "Deskout" -ErrorAction SilentlyContinue

# 3. Delete Shortcuts
Write-Host "[3/4] Deleting shortcuts..." -ForegroundColor Yellow
$ShortcutPath = "C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Deskout.lnk"
if (Test-Path $ShortcutPath) {
    Remove-Item -Path $ShortcutPath -Force
}

# 4. Remove Files
Write-Host "[4/4] Removing program files..." -ForegroundColor Yellow
$InstallDir = "C:\Program Files\Deskout"
if (Test-Path $InstallDir) {
    Remove-Item -Path $InstallDir -Recurse -Force
}

Write-Host ""
Write-Host "=========================================" -ForegroundColor Red
Write-Host "      UNINSTALL COMPLETED SUCCESS!       " -ForegroundColor Red
Write-Host "=========================================" -ForegroundColor Red
Write-Host "Deskout was successfully removed from your system." -ForegroundColor White
Write-Host ""
