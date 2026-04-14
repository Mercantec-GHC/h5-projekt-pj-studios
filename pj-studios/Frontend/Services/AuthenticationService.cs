using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Frontend.Services
{
    public class AuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        public const string ApiBaseUrl = "https://h5-projekt-pj-studios-1.onrender.com";
        private const string TokenStorageKey = "authToken";

        public AuthenticationService(HttpClient httpClient, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/api/User/login", new
                {
                    email,
                    password
                });

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var token = ExtractToken(content);

                    if (string.IsNullOrWhiteSpace(token))
                    {
                        return false;
                    }

                    await _jsRuntime.InvokeVoidAsync("authStorage.set", TokenStorageKey, token);
                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SignupAsync(string email, string password, string confirmPassword, string name)
        {
            try
            {
                if (password != confirmPassword)
                {
                    return false;
                }

                var response = await _httpClient.PostAsJsonAsync($"{ApiBaseUrl}/api/User/register", new
                {
                    username = name,
                    email,
                    password,
                    confirmedPassword = confirmPassword
                });

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task LogoutAsync()
        {
            await _jsRuntime.InvokeVoidAsync("authStorage.remove", TokenStorageKey);

            try
            {
                await _httpClient.PostAsync($"{ApiBaseUrl}/api/User/logout", null);
            }
            catch
            {
                // Ignore network errors while clearing the local session state.
            }
        }

        public async Task<string?> GetTokenAsync()
        {
            return await _jsRuntime.InvokeAsync<string?>("authStorage.get", TokenStorageKey);
        }

        private static string? ExtractToken(string responseContent)
        {
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(responseContent);

                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var tokenPropertyNames = new[] { "token", "jwt", "jwtToken", "accessToken" };

                    foreach (var propertyName in tokenPropertyNames)
                    {
                        if (document.RootElement.TryGetProperty(propertyName, out var tokenProperty) &&
                            tokenProperty.ValueKind == JsonValueKind.String)
                        {
                            return tokenProperty.GetString();
                        }
                    }
                }

                if (document.RootElement.ValueKind == JsonValueKind.String)
                {
                    return document.RootElement.GetString();
                }
            }
            catch (JsonException)
            {
                // Fall back to treating the response as a raw token string.
            }

            return responseContent.Trim().Trim('"');
        }
    }
}
