using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SnapBlast.Api.Models;

namespace SnapBlast.Api.Services;

public sealed class GeminiQuizService(HttpClient httpClient, IOptions<GeminiOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GeneratedQuizPayload> GenerateAsync(Stream document, string contentType, string? requestedTitle, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey)) throw new InvalidOperationException("Gemini:ApiKey is not configured.");
        await using var memory = new MemoryStream();
        await document.CopyToAsync(memory, cancellationToken);
        var encoded = Convert.ToBase64String(memory.ToArray());
        var prompt = "Extract the study material and generate a quiz. Return ONLY valid JSON matching this schema: {\"title\":\"string\",\"blocks\":[{\"term\":\"question or concept\",\"definition\":\"correct answer or explanation\",\"distractors\":[\"optional wrong answer\"]}]}. Create 2 to 32 high-quality pairs. Do not include markdown fences. " + (string.IsNullOrWhiteSpace(requestedTitle) ? string.Empty : $"Use this title: {requestedTitle.Trim()}.");
        var request = new
        {
            contents = new[] { new { parts = new object[] { new { text = prompt }, new { inline_data = new { mime_type = contentType, data = encoded } } } } },
            generationConfig = new { response_mime_type = "application/json", temperature = 0.3 }
        };
        using var response = await httpClient.PostAsJsonAsync($"v1beta/models/{settings.Model}:generateContent?key={Uri.EscapeDataString(settings.ApiKey)}", request, JsonOptions, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(body);
        var text = json.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
        var payload = JsonSerializer.Deserialize<GeneratedQuizPayload>(text ?? string.Empty, JsonOptions) ?? throw new InvalidOperationException("Gemini returned an empty quiz.");
        if (string.IsNullOrWhiteSpace(payload.Title) || payload.Blocks.Count is < 2 or > 32 || payload.Blocks.Any(block => string.IsNullOrWhiteSpace(block.Term) || string.IsNullOrWhiteSpace(block.Definition))) throw new InvalidOperationException("Gemini returned an invalid quiz payload.");
        return payload;
    }
}