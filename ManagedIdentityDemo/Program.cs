// MSAL.NET (Microsoft Authentication Library) is the official library for acquiring tokens
// from the Microsoft identity platform (Azure AD / Entra ID).
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.AppConfig;

// --- What is Managed Identity? ---
// Managed Identity is an Azure feature that provides your VM (or other Azure resource)
// with an automatic identity in Azure AD — no passwords, no secrets, no certificates needed.
// Azure handles the credential lifecycle for you behind the scenes.
//
// There are two types:
//   - System-assigned: tied to the VM's lifecycle (created/deleted with the VM)
//   - User-assigned:   a standalone Azure resource you attach to one or more VMs
//
// This demo uses System-assigned Managed Identity to access TWO different Azure apps/APIs.

// The scope tells Azure AD what resource we want to access.
// "https://management.azure.com/.default" = Azure Resource Manager (ARM) API.
var scope = "https://management.azure.com/.default";

// =====================================================================
// --- Demo 1: System-Assigned Managed Identity ---
// =====================================================================
// System-assigned identity is tied to THIS VM's lifecycle:
//   - Created automatically when you enable it on the VM
//   - Deleted automatically when the VM is deleted
//   - Cannot be shared with other VMs — it belongs to this VM only

// ManagedIdentityApplicationBuilder is MSAL's builder for managed identity scenarios.
// ManagedIdentityId.SystemAssigned tells MSAL to use the VM's built-in identity.
var systemMiApp = ManagedIdentityApplicationBuilder
    .Create(ManagedIdentityId.SystemAssigned)
    .Build();

try
{
    Console.WriteLine("========================================");
    Console.WriteLine(" Demo 1: System-Assigned Managed Identity");
    Console.WriteLine("========================================");
    Console.WriteLine();
    Console.WriteLine("Requesting token using System-Assigned Managed Identity...");

    // MSAL contacts the Azure Instance Metadata Service (IMDS) running on the VM
    // at http://169.254.169.254 to get a token. This endpoint is only accessible
    // from within the VM — no credentials leave the machine.
    var systemResult = await systemMiApp.AcquireTokenForManagedIdentity(scope).ExecuteAsync();

    // The result contains the access token (a JWT) that you can attach to HTTP requests
    // as a Bearer token: Authorization: Bearer eyJ0eXAiOiJKV1Q...
    Console.WriteLine("Token acquired successfully!");
    Console.WriteLine($"  Identity:   System-Assigned");
    Console.WriteLine($"  Resource:   Azure Resource Manager");
    Console.WriteLine($"  Expires on: {systemResult.ExpiresOn}");
    Console.WriteLine($"  Token src:  {systemResult.AuthenticationResultMetadata.TokenSource}");
    Console.WriteLine($"  Token:      {systemResult.AccessToken[..20]}...");
}
catch (MsalServiceException ex)
{
    // This will fail if:
    //   - The VM does not have a system-assigned managed identity enabled
    //   - The VM is not running in Azure
    //   - The identity does not have the required role on ARM resources
    Console.WriteLine($"System-Assigned MI failed: {ex.Message}");
}

// =====================================================================
// --- Demo 2: User-Assigned Managed Identity ---
// =====================================================================
// Unlike system-assigned (which is auto-created with the VM),
// a user-assigned managed identity is a standalone Azure resource that:
//   - You create independently in a resource group
//   - You assign to one or MORE VMs/resources (can be shared across VMs)
//   - It has its own lifecycle — deleting the VM does NOT delete it
//
// To use it, you need the Client ID of the user-assigned identity.
// Find it in the Azure portal: Managed Identity → Properties → Client ID

// Replace this with your actual user-assigned managed identity Client ID
var userAssignedClientId = "0350a2e9-d8dd-4d61-bfb8-c48115fbfc9e";

// Instead of ManagedIdentityId.SystemAssigned, we pass the Client ID
// of the specific user-assigned identity we want to use.
var userMiApp = ManagedIdentityApplicationBuilder
    .Create(ManagedIdentityId.WithUserAssignedClientId(userAssignedClientId))
    .Build();

try
{
    Console.WriteLine();
    Console.WriteLine("========================================");
    Console.WriteLine(" Demo 2: User-Assigned Managed Identity");
    Console.WriteLine("========================================");
    Console.WriteLine();
    Console.WriteLine("Requesting token using User-Assigned Managed Identity...");

    // Same mechanism — MSAL calls IMDS, but this time it specifies
    // which user-assigned identity to use via the client_id parameter.
    // This is important when a VM has multiple user-assigned identities attached.
    var userResult = await userMiApp.AcquireTokenForManagedIdentity(scope).ExecuteAsync();

    Console.WriteLine("Token acquired successfully!");
    Console.WriteLine($"  Identity:   User-Assigned (Client ID: {userAssignedClientId})");
    Console.WriteLine($"  Resource:   Azure Resource Manager");
    Console.WriteLine($"  Expires on: {userResult.ExpiresOn}");
    Console.WriteLine($"  Token src:  {userResult.AuthenticationResultMetadata.TokenSource}");
    Console.WriteLine($"  Token:      {userResult.AccessToken[..20]}...");
}
catch (MsalServiceException ex)
{
    // Common reasons for failure:
    //   - The client ID is wrong or doesn't match an identity assigned to this VM
    //   - The user-assigned identity is not attached to this VM
    //   - The identity doesn't have the required permissions on the target resource
    Console.WriteLine($"User-Assigned MI failed: {ex.Message}");
}

// =====================================================================
// --- Demo 3: Calling Azure REST API with the Token ---
// =====================================================================
// Now let's go beyond just acquiring a token — let's actually USE it!
// We'll call the Azure Resource Manager (ARM) REST API to list
// subscriptions and resource groups — proving the token works.
//
// The managed identity already has access to ARM (we got a token in Demo 1),
// so this works immediately — no extra permissions or setup needed.

try
{
    Console.WriteLine();
    Console.WriteLine("========================================");
    Console.WriteLine(" Demo 3: Calling Azure REST API");
    Console.WriteLine("========================================");
    Console.WriteLine();

    // We already proved we can get an ARM token in Demo 1.
    // Now let's actually USE that token to call the ARM API.
    var armApp = ManagedIdentityApplicationBuilder
        .Create(ManagedIdentityId.SystemAssigned)
        .Build();

    var armResult = await armApp.AcquireTokenForManagedIdentity(scope).ExecuteAsync();

    using var httpClient = new HttpClient();

    // Attach the token as a Bearer token in the Authorization header.
    // This is the standard OAuth 2.0 pattern for calling protected APIs:
    //   Authorization: Bearer eyJ0eXAiOiJKV1Q...
    httpClient.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", armResult.AccessToken);

    // --- Call 1: List Tenants ---
    // GET /tenants always returns data — every identity belongs to a tenant.
    // No role assignments needed, just a valid ARM token.
    Console.WriteLine("Calling ARM API: GET /tenants ...");
    var tenantResponse = await httpClient.GetAsync(
        "https://management.azure.com/tenants?api-version=2022-12-01");

    if (tenantResponse.IsSuccessStatusCode)
    {
        var tenantJson = await tenantResponse.Content.ReadAsStringAsync();
        using var tenantDoc = JsonDocument.Parse(tenantJson);
        var tenants = tenantDoc.RootElement.GetProperty("value");

        Console.WriteLine($"Found {tenants.GetArrayLength()} tenant(s):");
        foreach (var tenant in tenants.EnumerateArray())
        {
            var tenantId = tenant.GetProperty("tenantId").GetString();
            var displayName = tenant.TryGetProperty("displayName", out var dn) ? dn.GetString() : "(unnamed)";
            Console.WriteLine($"  - {displayName}");
            Console.WriteLine($"    Tenant ID: {tenantId}");
        }
    }
    else
    {
        Console.WriteLine($"Tenants call failed: {tenantResponse.StatusCode}");
    }

    // --- Call 2: List Subscriptions ---
    // GET /subscriptions returns all Azure subscriptions this identity can see.
    // This requires no extra role assignments — just a valid ARM token.
    Console.WriteLine();
    Console.WriteLine("Calling ARM API: GET /subscriptions ...");
    var subsResponse = await httpClient.GetAsync(
        "https://management.azure.com/subscriptions?api-version=2022-12-01");

    if (subsResponse.IsSuccessStatusCode)
    {
        var json = await subsResponse.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var subscriptions = doc.RootElement.GetProperty("value");

        Console.WriteLine($"Found {subscriptions.GetArrayLength()} subscription(s):");
        foreach (var sub in subscriptions.EnumerateArray())
        {
            var name = sub.GetProperty("displayName").GetString();
            var subId = sub.GetProperty("subscriptionId").GetString();
            var state = sub.GetProperty("state").GetString();
            Console.WriteLine($"  - {name}");
            Console.WriteLine($"    ID:    {subId}");
            Console.WriteLine($"    State: {state}");

            // --- Call 2: List Resource Groups in this subscription ---
            // This shows how you'd chain API calls using the same token.
            Console.WriteLine();
            Console.WriteLine($"  Listing resource groups in '{name}'...");
            var rgResponse = await httpClient.GetAsync(
                $"https://management.azure.com/subscriptions/{subId}/resourcegroups?api-version=2024-03-01");

            if (rgResponse.IsSuccessStatusCode)
            {
                var rgJson = await rgResponse.Content.ReadAsStringAsync();
                using var rgDoc = JsonDocument.Parse(rgJson);
                var groups = rgDoc.RootElement.GetProperty("value");

                Console.WriteLine($"  Found {groups.GetArrayLength()} resource group(s):");
                foreach (var rg in groups.EnumerateArray())
                {
                    var rgName = rg.GetProperty("name").GetString();
                    var location = rg.GetProperty("location").GetString();
                    Console.WriteLine($"    - {rgName} ({location})");
                }
            }
            else
            {
                Console.WriteLine($"  Could not list resource groups: {rgResponse.StatusCode}");
            }
        }
    }
    else
    {
        var errorBody = await subsResponse.Content.ReadAsStringAsync();
        Console.WriteLine($"ARM API call failed: {subsResponse.StatusCode}");
        Console.WriteLine($"  Response: {errorBody}");
    }
}
catch (MsalServiceException ex)
{
    Console.WriteLine($"ARM API call failed: {ex.Message}");
}

Console.WriteLine();
Console.WriteLine("========================================");
Console.WriteLine(" Comparison: System vs User-Assigned");
Console.WriteLine("========================================");
Console.WriteLine();
Console.WriteLine("System-Assigned:");
Console.WriteLine("  - Created/deleted with the VM");
Console.WriteLine("  - One per resource, cannot be shared");
Console.WriteLine("  - Simpler setup — just toggle it on");
Console.WriteLine();
Console.WriteLine("User-Assigned:");
Console.WriteLine("  - Independent lifecycle from the VM");
Console.WriteLine("  - Can be shared across multiple VMs");
Console.WriteLine("  - Better for multi-resource scenarios");
Console.WriteLine();
Console.WriteLine("Both use the same IMDS endpoint, same MSAL API — the only");
Console.WriteLine("difference is how you identify which identity to use.");
