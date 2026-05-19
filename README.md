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

## Setting Up the User-Assigned Managed Identity

Demo 2 requires a **user-assigned managed identity** attached to your VM. Follow these steps to create one and assign it:

### Step 1: Create a User-Assigned Managed Identity

1. Go to the [**User-Assigned Managed Identities**](https://portal.azure.com/#browse/Microsoft.ManagedIdentity%2FuserAssignedIdentities) page in the Azure portal
2. Click **+ Create**
3. Select your **Subscription** and **Resource group** (use the same resource group as your VM)
4. Select a **Region** (use the same region as your VM)
5. Enter a **Name** for the identity (e.g. `my-demo-identity`)
6. Click **Review + create** → **Create**

### Step 2: Copy the Client ID

1. Once created, open the managed identity resource
2. On the **Overview** page, copy the **Client ID** (a GUID like `xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx`)

### Step 3: Assign the Identity to Your VM

1. Go to your **Virtual Machine** in the Azure portal
2. In the left menu, go to **Settings** → **Identity**
3. Switch to the **User assigned** tab
4. Click **+ Add**
5. Search for and select the managed identity you created in Step 1
6. Click **Add**

### Step 4: Update the Code with Your Client ID

Open `ManagedIdentityDemo/Program.cs` and replace the existing client ID:

```csharp
var userAssignedClientId = "0350a2e9-d8dd-4d61-bfb8-c48115fbfc9e";
```

with the **Client ID** you copied in Step 2.

### Step 5: Run the Demo Again

```bash
dotnet run --project ManagedIdentityDemo
```

Both Demo 1 (System-Assigned) and Demo 2 (User-Assigned) should now succeed.

---

## Setting Up Graph API Permissions (Demo 3)

Demo 3 calls the **Microsoft Graph API** using the managed identity. For this to work, the identity needs **application permissions** on Microsoft Graph.

### Step 1: Find Your Managed Identity's Object ID

1. Go to your **Virtual Machine** in the Azure portal
2. Go to **Settings** → **Identity** → **System assigned** tab
3. Copy the **Object (principal) ID**

### Step 2: Grant Graph API Permissions via Azure Cloud Shell

Since application permissions on Microsoft Graph can only be granted via PowerShell/CLI (not the portal UI), open **Azure Cloud Shell** (PowerShell) and run:

```powershell
# Install the Microsoft Graph PowerShell module if not already installed
Install-Module Microsoft.Graph -Scope CurrentUser -Force

# Connect to Graph with admin consent
Connect-MgGraph -Scopes "AppRoleAssignment.ReadWrite.All"

# Set your managed identity's Object ID (from Step 1)
$miObjectId = "<YOUR-MANAGED-IDENTITY-OBJECT-ID>"

# Get the Microsoft Graph service principal
$graphSP = Get-MgServicePrincipal -Filter "displayName eq 'Microsoft Graph'" | Select-Object -First 1

# Find the Organization.Read.All app role
$appRole = $graphSP.AppRoles | Where-Object { $_.Value -eq "Organization.Read.All" }

# Grant the permission
New-MgServicePrincipalAppRoleAssignment `
    -ServicePrincipalId $miObjectId `
    -PrincipalId $miObjectId `
    -ResourceId $graphSP.Id `
    -AppRoleId $appRole.Id
```

### Step 3: Run the Demo

```bash
dotnet run --project ManagedIdentityDemo
```

Demo 3 should now show your **tenant name**, **tenant ID**, and **verified domains** from Microsoft Graph.

> **Note:** If you get a `403 Forbidden` error, the permission grant may take a few minutes to propagate. Wait and try again.

---

## What the code does

| Step | Description |
|------|-------------|
| 1 | Creates an MSAL managed identity app using `ManagedIdentityApplicationBuilder` |
| 2 | Requests an access token for Azure Resource Manager (Demo 1 & 2) |
| 3 | MSAL calls the Azure IMDS endpoint (`169.254.169.254`) on the VM |
| 4 | Prints the token details |
| 5 | Acquires a second token scoped to Microsoft Graph (Demo 3) |
| 6 | Calls `GET /organization` on the Graph API using the token as a Bearer header |
| 7 | Parses the JSON response and prints tenant info |

The token can be used as a **Bearer token** in HTTP requests to Azure APIs — no passwords or secrets needed.
