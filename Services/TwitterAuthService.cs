using RestSharp;
using RestSharp.Authenticators;
using System.Text.Json;

namespace BetterX_API.Services;

public class TwitterAuthService
{
    private static readonly string ClientKey = "";
    private static readonly string ClientSecret = "";
    private static readonly string CallbackUrl = "https://tpm28.com/betterx/callback";

    public async Task<(string Token, string AuthUrl)?> GetRequestTokenAsync()
    {
        try
        {
            var request = new RestRequest("https://api.twitter.com/oauth/request_token", Method.Post);
            request.AddParameter("oauth_callback", CallbackUrl);
            
            var options = new RestClientOptions
            {
                Authenticator = OAuth1Authenticator.ForRequestToken(ClientKey, ClientSecret)
            };
            
            using var authenticatedClient = new RestClient(options);
            var response = await authenticatedClient.ExecuteAsync(request);
            
            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
                return null;

            var responseParams = ParseQueryString(response.Content);
            var oauthToken = responseParams["oauth_token"];
            
            var authUrl = $"https://api.twitter.com/oauth/authorize?oauth_token={oauthToken}";
            return (oauthToken, authUrl);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting request token: {ex.Message}");
            return null;
        }
    }

    public async Task<Dictionary<string, string>?> GetAccessTokenAsync(string oauthToken, string oauthVerifier)
    {
        try
        {
            var request = new RestRequest("https://api.twitter.com/oauth/access_token", Method.Post);
            request.AddParameter("oauth_verifier", oauthVerifier);
            
            var options = new RestClientOptions
            {
                Authenticator = OAuth1Authenticator.ForAccessToken(
                    ClientKey, ClientSecret, oauthToken, "", oauthVerifier)
            };
            
            using var authenticatedClient = new RestClient(options);
            var response = await authenticatedClient.ExecuteAsync(request);
            
            if (!response.IsSuccessful || string.IsNullOrEmpty(response.Content))
                return null;

            return ParseQueryString(response.Content);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting access token: {ex.Message}");
            return null;
        }
    }

    private Dictionary<string, string> ParseQueryString(string query) =>
        query.Split('&')
            .Select(param => param.Split('='))
            .ToDictionary(parts => parts[0], parts => parts[1]);
}
