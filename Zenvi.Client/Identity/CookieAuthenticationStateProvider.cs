using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Zenvi.Client.Identity.Models;

namespace Zenvi.Client.Identity;

public class CookieAuthenticationStateProvider(IHttpClientFactory httpClientFactory)
    : AuthenticationStateProvider, IAccountManagement
{
        private readonly JsonSerializerOptions _jsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("Auth");
    private bool _authenticated = false;
    private readonly ClaimsPrincipal _unauthenticated = new(new ClaimsIdentity());

    public async Task<FormResult> RegisterAsync(string email, string name, string surname, string password)
    {
        var defaultDetail = new[] { "An unknown error prevented registration from succeeding." };

        try
        {
            var result = await _httpClient.PostAsJsonAsync("identity/register", new { email, name, surname, password });

            if (result.IsSuccessStatusCode)
                return new FormResult { Succeeded = true };

            var details = await result.Content.ReadAsStringAsync();
            var problemDetails = JsonDocument.Parse(details);
            var errors = problemDetails.RootElement.GetProperty("errors").EnumerateArray().Select(e => e.GetString() ?? string.Empty).Where(e => !string.IsNullOrEmpty(e)).ToList();

            return new FormResult { Succeeded = false, ErrorList = errors.Count > 0 ? errors.ToArray() : defaultDetail };
        }
        catch
        {
            return new FormResult { Succeeded = false, ErrorList = defaultDetail };
        }
    }

    public async Task<FormResult> LoginAsync(string email, string password)
    {
        try
        {
            var result = await _httpClient.PostAsJsonAsync("identity/login", new { email, password });

            if (result.IsSuccessStatusCode)
            {
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                return new FormResult { Succeeded = true };
            }
        }
        catch { }

        return new FormResult { Succeeded = false, ErrorList = new[] { "Invalid email and/or password." } };
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        _authenticated = false;
        var user = _unauthenticated;

        try
        {
            var userResponse = await _httpClient.GetAsync("identity/info");

            if (userResponse.IsSuccessStatusCode)
            {
                var userJson = await userResponse.Content.ReadAsStringAsync();
                var userInfo = JsonSerializer.Deserialize<UserInfo>(userJson, _jsonSerializerOptions);

                if (userInfo != null)
                {
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.Name, userInfo.Email),
                        new(ClaimTypes.Email, userInfo.Email)
                    };

                    var rolesResponse = await _httpClient.GetAsync("identity/roles");

                    if (rolesResponse.IsSuccessStatusCode)
                    {
                        var rolesJson = await rolesResponse.Content.ReadAsStringAsync();
                        var roles = JsonSerializer.Deserialize<RoleClaim[]>(rolesJson, _jsonSerializerOptions);

                        if (roles?.Length > 0)
                            claims.AddRange(roles.Select(role => new Claim(role.Type, role.Value, role.ValueType, role.Issuer, role.OriginalIssuer)));

                        var identity = new ClaimsIdentity(claims, nameof(CookieAuthenticationStateProvider));
                        user = new ClaimsPrincipal(identity);
                        _authenticated = true;
                    }
                }
            }
        }
        catch { }

        return new AuthenticationState(user);
    }

    public async Task LogoutAsync()
    {
        await _httpClient.PostAsync("identity/logout", null);
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task<bool> CheckAuthenticatedAsync()
    {
        await GetAuthenticationStateAsync();
        return _authenticated;
    }

    public class RoleClaim
    {
        public string? Issuer { get; set; }
        public string? OriginalIssuer { get; set; }
        public string? Type { get; set; }
        public string? Value { get; set; }
        public string? ValueType { get; set; }
    }
}