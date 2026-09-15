using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Messenger.Shared.Dtos;

namespace Messenger.Client.Services;

public class ApiService
{
    private readonly HttpClient _http = new();
    public const string BaseUrl = "http://localhost:5087";
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private void Authorize(string token) =>
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

    public async Task<System.Net.HttpStatusCode> RegisterAsync(string username, string password)
    {
        var r = await _http.PostAsJsonAsync($"{BaseUrl}/register",
            new RegisterRequest { Username = username, Password = password });
        return r.StatusCode;
    }

    public async Task<string?> LoginAsync(string username, string password)
    {
        var r = await _http.PostAsJsonAsync($"{BaseUrl}/login",
            new LoginRequest { Username = username, Password = password });
        if (!r.IsSuccessStatusCode) return null;
        var body = await r.Content.ReadFromJsonAsync<TokenResult>(JsonOpts);
        return body?.Token;
    }

    public async Task<List<UserDto>> GetUsersAsync(string token, string search)
    {
        Authorize(token);
        var url = $"{BaseUrl}/users?search={Uri.EscapeDataString(search)}";
        return await _http.GetFromJsonAsync<List<UserDto>>(url, JsonOpts) ?? [];
    }

    public async Task<List<ChatDto>> GetChatsAsync(string token)
    {
        Authorize(token);
        return await _http.GetFromJsonAsync<List<ChatDto>>($"{BaseUrl}/chats", JsonOpts) ?? [];
    }

    public async Task<ChatDto?> CreatePrivateChatAsync(string token, Guid otherUserId)
    {
        Authorize(token);
        var r = await _http.PostAsJsonAsync($"{BaseUrl}/chats/private",
            new CreatePrivateChatRequest { OtherUserId = otherUserId });
        return r.IsSuccessStatusCode
            ? await r.Content.ReadFromJsonAsync<ChatDto>(JsonOpts)
            : null;
    }

    private record TokenResult(string Token);
}
