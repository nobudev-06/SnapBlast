namespace SnapBlast.Api.Models;

public sealed class VisionOptions
{
    public string Endpoint { get; init; } = string.Empty;
    public string Key { get; init; } = string.Empty;
}

public sealed class GeminiOptions
{
    public string ApiKey { get; init; } = string.Empty;
    public string Model { get; init; } = "gemini-2.0-flash";
}

public sealed class MongoOptions
{
    public string ConnectionString { get; init; } = string.Empty;
    public string Database { get; init; } = "SnapBlast";
    public string QuizCollection { get; init; } = "quizzes";
    public string UserCollection { get; init; } = "users";
}

public sealed class UploadOptions
{
    public int MaxBytes { get; init; } = 10 * 1024 * 1024;
    public string[] AllowedContentTypes { get; init; } = ["image/jpeg", "image/png", "image/webp", "application/pdf"];
}

public sealed class AuthenticationOptions
{
    public string JwtSecret { get; init; } = string.Empty;
    public string Issuer { get; init; } = "SnapBlast.Api";
    public string Audience { get; init; } = "SnapBlast.Client";
    public int AccessTokenMinutes { get; init; } = 60;
}

public sealed class OcrOptions
{
    public string Provider { get; init; } = "GoogleGemini";
    public bool GoogleEnabled { get; init; } = true;
    public bool TesseractEnabled { get; init; } = true;
    public string TesseractExecutablePath { get; init; } = string.Empty;
    public string TessdataPath { get; init; } = string.Empty;
    public string Language { get; init; } = "eng";
}
