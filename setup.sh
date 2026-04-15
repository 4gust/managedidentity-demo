#!/bin/bash
# Setup script for Managed Identity Demo on Ubuntu VM
# Installs .NET SDK and restores project dependencies
# Usage: ./setup.sh

set -e

echo "======================================"
echo " Managed Identity Demo - Setup"
echo "======================================"
echo ""

# 1. Install .NET SDK
if command -v dotnet &> /dev/null; then
    echo "[OK] .NET SDK already installed: $(dotnet --version)"
else
    echo "[*] Installing .NET SDK 9.0..."
    wget -q https://dot.net/v1/dotnet-install.sh -O /tmp/dotnet-install.sh
    chmod +x /tmp/dotnet-install.sh
    /tmp/dotnet-install.sh --channel 9.0
    rm -f /tmp/dotnet-install.sh

    # Add dotnet to PATH for this session and permanently
    export DOTNET_ROOT="$HOME/.dotnet"
    export PATH="$DOTNET_ROOT:$PATH"

    # Add to .bashrc so it persists across terminals
    if ! grep -q ".dotnet" "$HOME/.bashrc" 2>/dev/null; then
        echo '' >> "$HOME/.bashrc"
        echo '# .NET SDK' >> "$HOME/.bashrc"
        echo 'export DOTNET_ROOT="$HOME/.dotnet"' >> "$HOME/.bashrc"
        echo 'export PATH="$DOTNET_ROOT:$PATH"' >> "$HOME/.bashrc"
    fi

    echo "[OK] .NET SDK installed: $(dotnet --version)"
fi

echo ""

# 2. Restore project dependencies (downloads NuGet packages)
echo "[*] Restoring project dependencies..."
dotnet restore ManagedIdentityDemo/
echo "[OK] Dependencies restored."

echo ""
echo "======================================"
echo " Setup complete! Running demo..."
echo "======================================"
echo ""

dotnet run --project ManagedIdentityDemo/
