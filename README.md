# Managed Identity Demo

A simple .NET console app that demonstrates how to acquire tokens using **Azure Managed Identity** with **MSAL.NET**.

The app shows both:
- **System-assigned** managed identity
- **User-assigned** managed identity

---

## Prerequisites

- An Azure VM (Ubuntu or Windows) with **Managed Identity enabled**
- VS Code with the **Remote - SSH** extension

---

## Steps (Ubuntu)

### 1. Connect to your VM from VS Code

Open VS Code → Press `Ctrl+Shift+P` → type **Remote-SSH: Connect to Host** → enter your VM's IP:

```
username@your-vm-ip
```

Open a folder (e.g. `/home/username/`) in VS Code.

### 2. Install Git

Open the VS Code terminal (`Ctrl+``) and run:

```bash
sudo apt update && sudo apt install -y git
```

### 3. Clone this repo

```bash
git clone <REPO-URL> managed-identity-demo
cd managed-identity-demo
```

### 4. Run the setup script

This installs .NET SDK and restores all dependencies:

```bash
chmod +x setup.sh
./setup.sh
```

### 5. Run the demo

```bash
dotnet run --project ManagedIdentityDemo
```

You should see output like:

```
Requesting token using Managed Identity (MSAL)...
Token acquired successfully!
Token expires on: ...
Token source: IdentityProvider
Token (first 20 chars): eyJ0eXAiOiJKV1QiLCJh...
```

---

## Steps (Windows)

### 1. Connect to your VM from VS Code

Open VS Code → Press `Ctrl+Shift+P` → type **Remote-SSH: Connect to Host** → enter your VM's IP:

```
username@your-vm-ip
```
it will ask for the password that you have set while creating the VM. Enter the password and connect.

Open a folder (e.g. `C:\Users\username\`) in VS Code. Or open any folder where you want to clone the repo.

### 2. Clone this repo

The setup script installs Git if needed, but if Git is already available:

```powershell
git clone <REPO-URL> managed-identity-demo
cd managed-identity-demo
```

If Git is not installed, download the repo as a ZIP from GitHub and extract it.

### 3. Run the setup script

This installs .NET SDK, Git, and restores all dependencies:

```powershell
.\setup.ps1
```

> If you get an execution policy error, run first:
> `Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy Bypass`

### 4. Run the demo

```powershell
dotnet run --project ManagedIdentityDemo
```

---

## Editing the User-Assigned Identity Client ID

If your VM has a **user-assigned managed identity**, open `ManagedIdentityDemo/Program.cs` and replace:

```csharp
var userAssignedClientId = "YOUR-USER-ASSIGNED-CLIENT-ID";
```

with your actual Client ID from the Azure portal:
**Managed Identity → Properties → Client ID**

---

## What the code does

| Step | Description |
|------|-------------|
| 1 | Creates an MSAL managed identity app using `ManagedIdentityApplicationBuilder` |
| 2 | Requests an access token for Azure Resource Manager |
| 3 | MSAL calls the Azure IMDS endpoint (`169.254.169.254`) on the VM |
| 4 | Prints the token details |

The token can be used as a **Bearer token** in HTTP requests to Azure APIs — no passwords or secrets needed.
