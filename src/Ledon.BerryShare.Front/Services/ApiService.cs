using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Ledon.BerryShare.Front.Services
{
    public class ApiService
    {
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private readonly HttpClient _httpClient;
        private readonly ITokenProvider _tokenProvider;
        private readonly NavigationManager _navigationManager;

        public ApiService(HttpClient httpClient, ITokenProvider tokenProvider, NavigationManager navigationManager)
        {
            _httpClient = httpClient;
            _tokenProvider = tokenProvider;
            _navigationManager = navigationManager;
        }

        private async Task AddAuthHeaderAsync()
        {
            var token = await _tokenProvider.GetTokenAsync();
            _httpClient.DefaultRequestHeaders.Authorization = null;
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        public async Task<T?> GetAsync<T>(string url)
        {
            await AddAuthHeaderAsync();
            var response = await _httpClient.GetAsync(url);
            await HandleAuthExpired(response);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine("GetAsync URL: " + url);
            Console.WriteLine("GetAsync response: " + json);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        public async Task<T?> PostAsync<T>(string url, object data)
        {
            await AddAuthHeaderAsync();
            var content = new StringContent(JsonSerializer.Serialize(data, _jsonOptions), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            await HandleAuthExpired(response);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            Console.WriteLine("PostAsync response: " + json);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        public async Task<T?> PutAsync<T>(string url, object data)
        {
            await AddAuthHeaderAsync();
            var content = new StringContent(JsonSerializer.Serialize(data, _jsonOptions), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync(url, content);
            await HandleAuthExpired(response);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        public async Task<T?> DeleteAsync<T>(string url)
        {
            await AddAuthHeaderAsync();
            var response = await _httpClient.DeleteAsync(url);
            await HandleAuthExpired(response);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }

        private async Task HandleAuthExpired(HttpResponseMessage response)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await _tokenProvider.RemoveTokenAsync();
                _navigationManager.NavigateTo("/login", true);
            }
        }
    }
}