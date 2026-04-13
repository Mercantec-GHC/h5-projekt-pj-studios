using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Frontend.Services
{
    public class JwtAuthenticationStateProvider : AuthenticationStateProvider
    {
        private const string TokenStorageKey = "authToken";
        private readonly IJSRuntime _jsRuntime;

        public JwtAuthenticationStateProvider(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var token = await _jsRuntime.InvokeAsync<string?>("authStorage.get", TokenStorageKey);

            if (!IsTokenValid(token))
            {
                await _jsRuntime.InvokeVoidAsync("authStorage.remove", TokenStorageKey);
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var identity = BuildClaimsIdentity(token!);
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }

        public async Task SetTokenAsync(string token)
        {
            await _jsRuntime.InvokeVoidAsync("authStorage.set", TokenStorageKey, token);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public async Task ClearTokenAsync()
        {
            await _jsRuntime.InvokeVoidAsync("authStorage.remove", TokenStorageKey);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public static bool IsTokenValid(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            try
            {
                var claims = DecodeClaims(token);

                if (!claims.TryGetValue("exp", out var expText))
                {
                    return true;
                }

                if (!long.TryParse(expText, out var expUnix))
                {
                    return true;
                }

                var expires = DateTimeOffset.FromUnixTimeSeconds(expUnix).UtcDateTime;

                return expires > DateTime.UtcNow;
            }
            catch
            {
                return false;
            }
        }

        private static ClaimsIdentity BuildClaimsIdentity(string token)
        {
            var claims = DecodeClaims(token)
                .Select(kvp => new Claim(kvp.Key, kvp.Value));

            return new ClaimsIdentity(claims, "jwt");
        }

        private static Dictionary<string, string> DecodeClaims(string token)
        {
            var parts = token.Split('.');

            if (parts.Length < 2)
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }

            var payloadBytes = Base64UrlDecode(parts[1]);
            var payloadJson = Encoding.UTF8.GetString(payloadBytes);
            using var document = JsonDocument.Parse(payloadJson);

            var claims = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in document.RootElement.EnumerateObject())
            {
                claims[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                    JsonValueKind.Number => property.Value.ToString(),
                    JsonValueKind.True => bool.TrueString,
                    JsonValueKind.False => bool.FalseString,
                    _ => property.Value.GetRawText()
                };
            }

            return claims;
        }

        private static byte[] Base64UrlDecode(string value)
        {
            var normalized = value.Replace('-', '+').Replace('_', '/');

            return (normalized.Length % 4) switch
            {
                2 => Convert.FromBase64String(normalized + "=="),
                3 => Convert.FromBase64String(normalized + "="),
                _ => Convert.FromBase64String(normalized)
            };
        }
    }
}
