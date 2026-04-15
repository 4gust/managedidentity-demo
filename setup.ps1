# Setup script for Managed Identity Demo on Windows VM
# Installs .NET SDK, Git, and restores project dependencies
# Usage: .\setup.ps1

$ErrorActionPreference = "Stop"

Write-Host "======================================" -ForegroundColor Cyan
Write-Host " Managed Identity Demo - Setup"        -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""

$dotnetDir = Join-Path $env:LOCALAPPDATA "Microsoft\dotnet"

# 1. Install .NET SDK
if (Get-Command dotnet -ErrorAction SilentlyContinue) {
    Write-Host "[OK] .NET SDK already installed: $(dotnet --version)" -ForegroundColor Green
} else {
    Write-Host "[*] Installing .NET SDK 9.0..." -ForegroundColor Cyan
    $scriptPath = Join-Path $env:TEMP "dotnet-install.ps1"
    Invoke-WebRequest -Uri "https://dot.net/v1/dotnet-install.ps1" -OutFile $scriptPath
    & $scriptPath -Channel 9.0
    Remove-Item $scriptPath -ErrorAction SilentlyContinue

    # Add to current session
    $env:PATH = "$dotnetDir;$env:PATH"

    # Add to Machine PATH
    $machinePath = [Environment]::GetEnvironmentVariable("PATH", "Machine")
    if ($machinePath -notlike "*$dotnetDir*") {
        [Environment]::SetEnvironmentVariable("PATH", "$machinePath;$dotnetDir", "Machine")
    }

    # Add to User PATH
    $userPath = [Environment]::GetEnvironmentVariable("PATH", "User")
    if ($userPath -notlike "*$dotnetDir*") {
        [Environment]::SetEnvironmentVariable("PATH", "$userPath;$dotnetDir", "User")
    }

    # Add to PowerShell profile
    if (!(Test-Path $PROFILE)) { New-Item -Path $PROFILE -Force | Out-Null }
    $profileContent = Get-Content $PROFILE -Raw -ErrorAction SilentlyContinue
    if ($profileContent -notlike "*$dotnetDir*") {
        Add-Content -Path $PROFILE -Value "`nif (Test-Path '$dotnetDir\dotnet.exe') { `$env:PATH = '$dotnetDir;' + `$env:PATH }"
    }

    Write-Host "[OK] .NET SDK installed: $(dotnet --version)" -ForegroundColor Green
}

Write-Host ""

# 2. Install Git
$gitDir = "C:\Program Files\Git\cmd"
if (Get-Command git -ErrorAction SilentlyContinue) {
    Write-Host "[OK] Git already installed: $(git --version)" -ForegroundColor Green
} else {
    Write-Host "[*] Downloading Git for Windows..." -ForegroundColor Cyan
    $gitInstaller = Join-Path $env:TEMP "git-installer.exe"
    Invoke-WebRequest -Uri "https://github.com/git-for-windows/git/releases/download/v2.47.1.windows.2/Git-2.47.1.2-64-bit.exe" -OutFile $gitInstaller

    Write-Host "[*] Installing Git silently..." -ForegroundColor Cyan
    Start-Process -FilePath $gitInstaller -ArgumentList "/VERYSILENT /NORESTART /NOCANCEL /SP- /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS" -Wait
    Remove-Item $gitInstaller -ErrorAction SilentlyContinue

    $env:PATH = "$gitDir;$env:PATH"
    Write-Host "[OK] $(git --version)" -ForegroundColor Green
}

Write-Host ""

# 3. Restore project dependencies
Write-Host "[*] Restoring project dependencies..." -ForegroundColor Cyan
dotnet restore ManagedIdentityDemo/
Write-Host "[OK] Dependencies restored." -ForegroundColor Green

Write-Host ""
Write-Host "======================================" -ForegroundColor Cyan
Write-Host " Setup complete!"                      -ForegroundColor Cyan
Write-Host "======================================" -ForegroundColor Cyan
Write-Host ""
Write-Host " Run the demo with:" -ForegroundColor Yellow
Write-Host "   dotnet run --project ManagedIdentityDemo" -ForegroundColor White
Write-Host ""
