using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace SnapBlast.Services;

public sealed class QuizApiClient(HttpClient httpClient)
{
    private const string TokenKey = "snapblast_access_token";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Name of the signed-in user (empty when signed out).</summary>
    public string DisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// Signs in or registers. Throws InvalidOperationException with the server's message
    /// when the credentials are rejected, and HttpRequestException when the server is unreachable.
    /// </summary>
    public async Task<AuthResult> AuthenticateAsync(string email, string password, string displayName, bool register, CancellationToken cancellationToken)
    {
        var endpoint = register ? "api/auth/register" : "api/auth/login";
        using var response = register
            ? await httpClient.PostAsJsonAsync(endpoint, new RegisterRequest(email, password, displayName), JsonOptions, cancellationToken)
            : await httpClient.PostAsJsonAsync(endpoint, new LoginRequest(email, password), JsonOptions, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(DescribeError(response.StatusCode, body));
        }

        var result = JsonSerializer.Deserialize<AuthResult>(body, JsonOptions)
            ?? throw new InvalidOperationException("The API returned an empty authentication response.");
        await SecureStorage.Default.SetAsync(TokenKey, result.AccessToken);
        httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
        DisplayName = string.IsNullOrWhiteSpace(result.DisplayName) ? result.Email : result.DisplayName;
        return result;
    }

    public void SignOut()
    {
        SecureStorage.Default.Remove(TokenKey);
        httpClient.DefaultRequestHeaders.Authorization = null;
        DisplayName = string.Empty;
    }

    /// <summary>Block Blast: extracts text and generates term/definition pairs.</summary>
    public Task<GeneratedQuizResult?> GenerateQuizAsync(FileResult file, string? title, CancellationToken cancellationToken) =>
        PostDocumentAsync("api/generation/quiz-from-document", file, title, cancellationToken);

    /// <summary>Multiple choice: extracts text and generates questions with wrong answers (distractors).</summary>
    public Task<GeneratedQuizResult?> GenerateMultipleChoiceAsync(FileResult file, string? title, CancellationToken cancellationToken) =>
        PostDocumentAsync("api/generation/multiple-choice-from-document", file, title, cancellationToken);

    private async Task<GeneratedQuizResult?> PostDocumentAsync(string endpoint, FileResult file, string? title, CancellationToken cancellationToken)
    {
        await using var stream = await file.OpenReadAsync();
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");
        content.Add(fileContent, "file", file.FileName);
        content.Add(new StringContent(title ?? string.Empty), "Title");
        content.Add(new StringContent("false"), "Save");

        var token = await SecureStorage.Default.GetAsync(TokenKey);
        if (!string.IsNullOrWhiteSpace(token))
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await httpClient.PostAsync(endpoint, content, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new InvalidOperationException("Your session has expired. Go back to the menu, sign out, and sign in again.");
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(DescribeError(response.StatusCode, responseBody));
        }

        return JsonSerializer.Deserialize<GeneratedQuizResult>(responseBody, JsonOptions);
    }

    private static string DescribeError(HttpStatusCode status, string body)
    {
        var text = body.Trim();
        if (text.Length > 250)
        {
            text = text[..250] + "...";
        }

        return string.IsNullOrWhiteSpace(text) ? $"Request failed ({(int)status} {status})." : text;
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
    public IReadOnlyList<string> Distractors { get; init; } = [];
}