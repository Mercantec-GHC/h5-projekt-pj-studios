using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Frontend.Services
{
    public class AuthenticationService
    {
        private readonly HttpClient _httpClient;
        private readonly IJSRuntime _jsRuntime;
        public const string ApiBaseUrl = "https://localhost:7087";
        private const string TokenStorageKey = "authToken";
        private const string UserEmailStorageKey = "authEmail";
        private const string UsernameStorageKey = "authUsername";

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
                    var username = ExtractUsername(content);

                    await _jsRuntime.InvokeVoidAsync("authStorage.set", UserEmailStorageKey, email);

                    if (!string.IsNullOrWhiteSpace(username))
                    {
                        await _jsRuntime.InvokeVoidAsync("authStorage.set", UsernameStorageKey, username);
                    }

                    if (!string.IsNullOrWhiteSpace(token))
                    {
                        await _jsRuntime.InvokeVoidAsync("authStorage.set", TokenStorageKey, token);
                    }

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
            await _jsRuntime.InvokeVoidAsync("authStorage.remove", UserEmailStorageKey);
            await _jsRuntime.InvokeVoidAsync("authStorage.remove", UsernameStorageKey);

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

        public async Task<string?> GetStoredEmailAsync()
        {
            return await _jsRuntime.InvokeAsync<string?>("authStorage.get", UserEmailStorageKey);
        }

        public async Task<string?> GetStoredUsernameAsync()
        {
            return await _jsRuntime.InvokeAsync<string?>("authStorage.get", UsernameStorageKey);
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

                var token = FindStringPropertyRecursive(document.RootElement, "token", "jwt", "jwtToken", "accessToken");

                if (!string.IsNullOrWhiteSpace(token))
                {
                    return token;
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

        private static string? ExtractUsername(string responseContent)
        {
            if (string.IsNullOrWhiteSpace(responseContent))
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(responseContent);
                return FindStringPropertyRecursive(document.RootElement, "username", "userName", "name");
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private static string? FindStringPropertyRecursive(JsonElement element, params string[] propertyNames)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                foreach (var propertyName in propertyNames)
                {
                    if (element.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.String)
                    {
                        var text = value.GetString();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            return text;
                        }
                    }
                }

                foreach (var property in element.EnumerateObject())
                {
                    var found = FindStringPropertyRecursive(property.Value, propertyNames);
                    if (!string.IsNullOrWhiteSpace(found))
                    {
                        return found;
                    }
                }
            }

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    var found = FindStringPropertyRecursive(item, propertyNames);
                    if (!string.IsNullOrWhiteSpace(found))
                    {
                        return found;
                    }
                }
            }

            return null;
        }
    }
}
