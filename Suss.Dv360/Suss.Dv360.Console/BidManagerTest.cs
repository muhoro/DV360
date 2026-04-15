// Quick script to test Bid Manager API access with OAuth 2.0
// Run with: dotnet run --project Suss.Dv360.Console -- bidmanager

using Google.Apis.Auth.OAuth2;
using Google.Apis.DoubleClickBidManager.v2;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace Suss.Dv360.Console;

public static class BidManagerTest
{
    public static async Task RunAsync(string clientId, string clientSecret, long advertiserId)
    {
        System.Console.WriteLine("═══════════════════════════════════════════════════════════════");
        System.Console.WriteLine("  DV360 Bid Manager API - OAuth 2.0 Access Test");
        System.Console.WriteLine("═══════════════════════════════════════════════════════════════");
        System.Console.WriteLine();

        try
        {
            // OAuth 2.0 scopes required for DV360 + Bid Manager
            var scopes = new[]
            {
                "https://www.googleapis.com/auth/display-video",
                "https://www.googleapis.com/auth/doubleclickbidmanager"
            };

            // Launch OAuth flow - opens browser on first run, reuses token thereafter
            System.Console.WriteLine("Initiating OAuth 2.0 authorization...");
            System.Console.WriteLine("(A browser window will open if this is your first time)");
            System.Console.WriteLine();

            var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret
                },
                scopes,
                "user",
                CancellationToken.None,
                new FileDataStore("tokens"));

            System.Console.WriteLine("✅ OAuth 2.0 authorization successful!");
            System.Console.WriteLine();

            // Create the Bid Manager service
            var service = new DoubleClickBidManagerService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "Suss.Dv360.BidManagerTest"
            });

            // List existing queries to verify API access
            System.Console.WriteLine("Testing Bid Manager API access...");
            System.Console.WriteLine();

            var queryList = await service.Queries.List().ExecuteAsync();

            System.Console.WriteLine("✅ Bid Manager API access confirmed!");
            System.Console.WriteLine();
            
            var queryCount = queryList.Queries?.Count ?? 0;
            System.Console.WriteLine($"Found {queryCount} existing queries in your account.");
            
            if (queryList.Queries != null && queryList.Queries.Count > 0)
            {
                System.Console.WriteLine();
                System.Console.WriteLine("Recent queries:");
                System.Console.WriteLine("┌────────────────┬────────────────────────────────────────────────────┐");
                System.Console.WriteLine("│ Query ID       │ Title                                              │");
                System.Console.WriteLine("├────────────────┼────────────────────────────────────────────────────┤");

                foreach (var query in queryList.Queries.Take(10))
                {
                    var queryId = query.QueryId?.ToString() ?? "N/A";
                    var title = Truncate(query.Metadata?.Title ?? "Untitled", 50);
                    System.Console.WriteLine($"│ {queryId,-14} │ {title,-50} │");
                }

                System.Console.WriteLine("└────────────────┴────────────────────────────────────────────────────┘");
            }

            System.Console.WriteLine();
            System.Console.WriteLine($"You can now use IReportingService with AdvertiserId: {advertiserId}");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ Error: {ex.Message}");
            System.Console.WriteLine();
            
            if (ex.Message.Contains("insufficient authentication scopes"))
            {
                System.Console.WriteLine("The OAuth token doesn't have the required Bid Manager scope.");
                System.Console.WriteLine();
                System.Console.WriteLine("To fix this:");
                System.Console.WriteLine("  1. Go to Google Cloud Console → APIs & Services → OAuth consent screen");
                System.Console.WriteLine("  2. Click 'Edit App' and add the scope:");
                System.Console.WriteLine("     https://www.googleapis.com/auth/doubleclickbidmanager");
                System.Console.WriteLine("  3. Delete the 'tokens' folder in this directory to clear cached tokens");
                System.Console.WriteLine("  4. Re-run this command to get fresh OAuth consent with both scopes");
            }
            else
            {
                System.Console.WriteLine("Make sure:");
                System.Console.WriteLine("  1. Your OAuth Client ID and Secret are correct in appsettings.json");
                System.Console.WriteLine("  2. The DoubleClick Bid Manager API is enabled in Google Cloud Console");
                System.Console.WriteLine("  3. Your Google account has DV360 partner/advertiser access");
                System.Console.WriteLine("  4. The OAuth consent screen is configured for your project");
            }
        }
    }

    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..(maxLength - 3)] + "...";
    }
}
