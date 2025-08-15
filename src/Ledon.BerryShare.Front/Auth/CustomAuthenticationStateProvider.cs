using System;
using Ledon.BerryShare.Front.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace Ledon.BerryShare.Front.Auth;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly ITokenProvider _tokenProvider;

    public CustomAuthenticationStateProvider(ITokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var token = await _tokenProvider.GetTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            // 未登录，返回空身份
            var anonymous = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity());
            return new AuthenticationState(anonymous);
        }

        // 解析 token，假设为 JWT
        var claims = ParseClaimsFromJwt(token);
        var identity = new System.Security.Claims.ClaimsIdentity(claims, "jwt");
        var user = new System.Security.Claims.ClaimsPrincipal(identity);
        return new AuthenticationState(user);
    }

    // 新增: 登录后立即通知 UI 刷新
    public async Task MarkUserAsAuthenticated(string token)
    {
        if (!string.IsNullOrWhiteSpace(token))
        {
            await _tokenProvider.SetTokenAsync(token); // 确保存储
            var claims = ParseClaimsFromJwt(token);
            var identity = new System.Security.Claims.ClaimsIdentity(claims, "jwt");
            var user = new System.Security.Claims.ClaimsPrincipal(identity);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
        }
        else
        {
            await MarkUserAsLoggedOut();
        }
    }

    // 新增: 注销并通知
    public async Task MarkUserAsLoggedOut()
    {
        await _tokenProvider.RemoveTokenAsync();
        var anonymous = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymous)));
    }

    private IEnumerable<System.Security.Claims.Claim> ParseClaimsFromJwt(string jwt)
    {
        var claims = new List<System.Security.Claims.Claim>();
        var segments = jwt.Split('.');
        if (segments.Length < 2) return claims; // 简单保护
        var payload = segments[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
        if (keyValuePairs != null)
        {
            foreach (var kvp in keyValuePairs)
            {
                claims.Add(new System.Security.Claims.Claim(kvp.Key, kvp.Value?.ToString() ?? ""));
            }
        }
        return claims;
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
