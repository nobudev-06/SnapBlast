using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SnapBlast.Models;

namespace SnapBlast.Services;

public sealed class QuizApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<AuthResult> AuthenticateAsync(string email, string password, string displayName, bool register, CancellationToken cancellationToken)
    {
        var endpoint = register ? "api/auth/register" : "api/auth/login";
        using var response = register
            ? await httpClient.PostAsJsonAsync(endpoint, new RegisterRequest(email, password, displayName), JsonOptions, cancellationToken)
            : await httpClient.PostAsJsonAsync(endpoint, new LoginRequest(email, password), JsonOptions, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = JsonSerializer.Deserialize<AuthResult>(body, JsonOptions)
            ?? throw new InvalidOperationException("The API returned an empty authentication response.");
        await SecureStorage.Default.SetAsync("snapblast_access_token", result.AccessToken);
        return result;
    }

    public async Task<GeneratedQuizResult?> GenerateQuizAsync(FileResult file, string? title, CancellationToken cancellationToken)
    {
        await using var stream = await file.OpenReadAsync();
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
        content.Add(fileContent, "file", file.FileName);
        content.Add(new StringContent(title ?? string.Empty), "Title");
        content.Add(new StringContent("false"), "Save");

        var token = await SecureStorage.Default.GetAsync("snapblast_access_token");
        if (!string.IsNullOrWhiteSpace(token))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await httpClient.PostAsync("api/generation/quiz-from-document", content, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException("Sign in to SnapBlast before generating a quiz from a document.");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();
        return JsonSerializer.Deserialize<GeneratedQuizResult>(responseBody, JsonOptions);
    }
}

public sealed class AuthResult
{
    public string AccessToken { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}

public sealed class GeneratedQuizResult
{
    public string Title { get; init; } = string.Empty;
    public IReadOnlyList<GeneratedBlock> Blocks { get; init; } = [];
}

internal sealed record RegisterRequest(string Email, string Password, string DisplayName);

internal sealed record LoginRequest(string Email, string Password);

public sealed class GeneratedBlock
{
    public string Term { get; init; } = string.Empty;
    public string Definition { get; init; } = string.Empty;
}
