using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SnapBlast.Api.Models;

namespace SnapBlast.Api.Services;

public sealed class GeminiQuizService(HttpClient httpClient, IOptions<GeminiOptions> options)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Block Blast: term/definition pairs (distractors optional).</summary>
    public async Task<GeneratedQuizPayload> GenerateAsync(Stream document, string contentType, string? requestedTitle, CancellationToken cancellationToken)
    {
        var prompt = "Extract the study material and generate a quiz. Return ONLY valid JSON matching this schema: {\"title\":\"string\",\"blocks\":[{\"term\":\"question or concept\",\"definition\":\"correct answer or explanation\",\"distractors\":[\"optional wrong answer\"]}]}. Create 2 to 32 high-quality pairs. Do not include markdown fences. " + TitleHint(requestedTitle);
        var payload = await RequestAsync(document, contentType, prompt, cancellationToken);
        if (string.IsNullOrWhiteSpace(payload.Title) || payload.Blocks.Count is < 2 or > 32 || payload.Blocks.Any(block => string.IsNullOrWhiteSpace(block.Term) || string.IsNullOrWhiteSpace(block.Definition)))
        {
            throw new InvalidOperationException("Gemini returned an invalid quiz payload.");
        }

        return payload;
    }

    /// <summary>Multiple choice: each block is a question, its correct answer, and up to 3 wrong answers.</summary>
    public async Task<GeneratedQuizPayload> GenerateMultipleChoiceAsync(Stream document, string contentType, string? requestedTitle, CancellationToken cancellationToken)
    {
        var prompt = "Extract the study material from this document and write multiple-choice quiz questions about it. Return ONLY valid JSON matching this schema: {\"title\":\"string\",\"blocks\":[{\"term\":\"the question text\",\"definition\":\"the single correct answer\",\"distractors\":[\"wrong answer 1\",\"wrong answer 2\",\"wrong answer 3\"]}]}. Create 5 to 20 questions. Every question must have exactly 3 plausible but clearly incorrect distractors that are similar in length and style to the correct answer. Do not include markdown fences. " + TitleHint(requestedTitle);
        var payload = await RequestAsync(document, contentType, prompt, cancellationToken);

        var blocks = payload.Blocks.Select(ToMultipleChoiceBlock).OfType<GameBlockResponse>().Take(32).ToList();
        if (string.IsNullOrWhiteSpace(payload.Title) || blocks.Count < 2)
        {
            throw new InvalidOperationException("Gemini returned too few usable multiple-choice questions.");
        }

        return new GeneratedQuizPayload { Title = payload.Title.Trim(), Blocks = blocks };
    }

    // Cleans one AI-generated question: trims, removes blank/duplicate/identical-to-answer distractors, keeps at most 3.
    private static GameBlockResponse? ToMultipleChoiceBlock(GameBlockResponse block)
    {
        var question = block.Term?.Trim();
        var answer = block.Definition?.Trim();
        if (string.IsNullOrEmpty(question) || string.IsNullOrEmpty(answer)) return null;

        var wrong = (block.Distractors ?? Array.Empty<string>())
            .Select(item => item?.Trim() ?? string.Empty)
            .Where(item => item.Length > 0 && !item.Equals(answer, StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();

        return wrong.Count < 2 ? null : new GameBlockResponse { Term = question, Definition = answer, Distractors = wrong };
    }

    private static string TitleHint(string? requestedTitle) =>
        string.IsNullOrWhiteSpace(requestedTitle) ? string.Empty : $"Use this title: {requestedTitle.Trim()}.";

    private async Task<GeneratedQuizPayload> RequestAsync(Stream document, string contentType, string prompt, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey)) throw new InvalidOperationException("Gemini:ApiKey is not configured.");

        await using var memory = new MemoryStream();
        await document.CopyToAsync(memory, cancellationToken);
        var encoded = Convert.ToBase64String(memory.ToArray());
        var request = new
        {
            contents = new[] { new { parts = new object[] { new { text = prompt }, new { inline_data = new { mime_type = contentType, data = encoded } } } } },
            generationConfig = new { response_mime_type = "application/json", temperature = 0.3 }
        };

        using var response = await httpClient.PostAsJsonAsync($"v1beta/models/{settings.Model}:generateContent?key={Uri.EscapeDataString(settings.ApiKey)}", request, JsonOptions, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            // Surface Google's own error text (bad key, retired model name, quota...) instead of a bare status code.
            throw new HttpRequestException($"Gemini returned {(int)response.StatusCode} {response.ReasonPhrase}: {body[..Math.Min(body.Length, 300)]}");
        }

        using var json = JsonDocument.Parse(body);
        if (!json.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("Gemini returned no result (the document may have been blocked).");
        }

        var text = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
        return JsonSerializer.Deserialize<GeneratedQuizPayload>(StripCodeFence(text ?? string.Empty), JsonOptions)
            ?? throw new InvalidOperationException("Gemini returned an empty quiz.");
    }

    // Models sometimes wrap JSON in ```json fences even when told not to.
    private static string StripCodeFence(string text)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith("```")) return trimmed;
        var firstNewLine = trimmed.IndexOf('\n');
        var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        return firstNewLine >= 0 && lastFence > firstNewLine ? trimmed[(firstNewLine + 1)..lastFence].Trim() : trimmed;
    }
}