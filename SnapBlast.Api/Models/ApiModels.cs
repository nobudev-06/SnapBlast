namespace SnapBlast.Api.Models;

public sealed class QuizResponse
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string ImageHash { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public IReadOnlyList<GameBlockResponse> Blocks { get; init; } = [];
}

public sealed class RegisterRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public sealed class AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
}

public sealed class SaveQuizRequest
{
    public string Title { get; init; } = string.Empty;
    public IReadOnlyList<GameBlockResponse> Blocks { get; init; } = [];
}

public sealed class GenerateQuizRequest
{
    public string? Title { get; init; }
    public bool Save { get; init; }
}

public sealed class GameBlockResponse
{
    public string Term { get; init; } = string.Empty;
    public string Definition { get; init; } = string.Empty;
    public IReadOnlyList<string> Distractors { get; init; } = [];
}

public sealed class GeneratedQuizPayload
{
    public string Title { get; init; } = string.Empty;
    public IReadOnlyList<GameBlockResponse> Blocks { get; init; } = [];
}
