using System.Net.Http.Json;
using System.Text.Json;

namespace Frontend.Services
{
    public class AuthenticationService
    {
        private readonly HttpClient _httpClient;
        private const string ApiBaseUrl = "https://localhost:7087";

        public AuthenticationService(HttpClient httpClient)
        {
            _httpClient = httpClient;
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
                    // Store the token in local storage or session
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
            await _httpClient.PostAsync($"{ApiBaseUrl}/api/User/logout", null);
        }
    }
}
