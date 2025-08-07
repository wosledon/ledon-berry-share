using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace Ledon.BerryShare.Front.Services
{
    public interface ITokenProvider
    {
        Task<string?> GetTokenAsync();
        Task SetTokenAsync(string token);
        Task RemoveTokenAsync();
    }

    public class TokenProvider : ITokenProvider
    {
        private readonly IJSRuntime _jsRuntime;
        public TokenProvider(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }
        public async Task<string?> GetTokenAsync()
        {
            return await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "token");
        }
        public async Task SetTokenAsync(string token)
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "token", token);
        }
        public async Task RemoveTokenAsync()
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "token");
        }
    }
}
