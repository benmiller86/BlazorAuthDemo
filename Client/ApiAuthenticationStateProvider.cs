using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;

namespace BlazorAuthDemo.Client;
public class ApiAuthenticationStateProvider : AuthenticationStateProvider
{
    public const string TokenKey = "authentication-token";
    private const string AuthenticationType = "JWT";
    private const string AuthenticationScheme = "bearer";

    private readonly HttpClient http;
    private readonly ILocalStorageService localStorage;

    public ApiAuthenticationStateProvider(HttpClient http, ILocalStorageService localStorage)
    {
        this.http = http;
        this.localStorage = localStorage;
    }

    private static ClaimsPrincipal GetAnonymousUser()
    {
        Claim[] claims = new[]
        {
            new Claim("Anonymous", bool.TrueString),
        };
        ClaimsIdentity identity = new(claims);
        ClaimsPrincipal principal = new(identity);
        return principal;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        string token = await this.localStorage.GetItemAsync<string>(TokenKey);

        if (string.IsNullOrWhiteSpace(token))
            return new AuthenticationState(GetAnonymousUser());

        this.http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AuthenticationScheme, token); 
        return GetAuthStateFromJwt(token);
    }

    public void MarkUserAsAuthenticated(string token)
    {
        AuthenticationState authState = GetAuthStateFromJwt(token);
        base.NotifyAuthenticationStateChanged(Task.FromResult(authState));
    }

    public void MarkUserAsSignedOut()
    {
        AuthenticationState authState = new(GetAnonymousUser());
        base.NotifyAuthenticationStateChanged(Task.FromResult(authState));
    }

    private static AuthenticationState GetAuthStateFromJwt(string token)
    {
        IEnumerable<Claim> claims = ParseClaimsFromJwt(token);
        ClaimsIdentity identity = new(claims, AuthenticationType);
        ClaimsPrincipal user = new(identity);
        AuthenticationState authState = new(user);
        return authState;
    }

    /// <summary>
    /// https://chrissainty.com/securing-your-blazor-apps-authentication-with-clientside-blazor-using-webapi-aspnet-core-identity/.
    /// </summary>
    /// <param name="token">The JWT.</param>
    /// <returns></returns>
    private static IEnumerable<Claim> ParseClaimsFromJwt(string token)
    {
        List<Claim> claims = new();
        string payload = token.Split('.')[1];
        byte[] jsonBytes = ParseBase64WithoutPadding(payload);
        Dictionary<string, object>? keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs is not null && keyValuePairs.TryGetValue(ClaimTypes.Role, out object? roles) && roles is not null)
        {
            if (roles.ToString()!.Trim().StartsWith("["))
            {
                string[] parsedRoles = JsonSerializer.Deserialize<string[]>(roles.ToString()!) ?? Array.Empty<string>();

                foreach (string parsedRole in parsedRoles)
                    claims.Add(new Claim(ClaimTypes.Role, parsedRole));
            }
            else
            {
                claims.Add(new Claim(ClaimTypes.Role, roles.ToString()!));
            }
        }

        _ = keyValuePairs?.Remove(ClaimTypes.Role);

        if (keyValuePairs is not null)
            claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value?.ToString() ?? string.Empty)));

        return claims;
    }

    private static byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
