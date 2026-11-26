using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using SnapBlast.Api.Models;
using SnapBlast.Api.Services;

namespace SnapBlast.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/generation")]
public sealed class GenerationController(GeminiQuizService generator, IQuizRepository quizzes, IOptions<UploadOptions> uploadOptions) : ControllerBase
{
    private delegate Task<GeneratedQuizPayload> Generator(Stream document, string contentType, string? title, CancellationToken cancellationToken);

    // Block Blast: term/definition pairs
    [HttpPost("quiz-from-document")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public Task<ActionResult<QuizResponse>> Generate(IFormFile file, [FromForm] GenerateQuizRequest request, CancellationToken cancellationToken) =>
        BuildQuizAsync(file, request, generator.GenerateAsync, cancellationToken);

    // Multiple choice: question + correct answer + wrong answers
    [HttpPost("multiple-choice-from-document")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public Task<ActionResult<QuizResponse>> GenerateMultipleChoice(IFormFile file, [FromForm] GenerateQuizRequest request, CancellationToken cancellationToken) =>
        BuildQuizAsync(file, request, generator.GenerateMultipleChoiceAsync, cancellationToken);

    private async Task<ActionResult<QuizResponse>> BuildQuizAsync(IFormFile file, GenerateQuizRequest request, Generator generate, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return BadRequest("A document is required.");
        var settings = uploadOptions.Value;
        if (file.Length > settings.MaxBytes || !settings.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase)) return BadRequest("The document type or size is not allowed.");

        GeneratedQuizPayload generated;
        try
        {
            await using var stream = file.OpenReadStream();
            generated = await generate(stream, file.ContentType, request.Title, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidOperationException or JsonException or KeyNotFoundException)
        {
            // Return the reason to the app instead of a blank 500.
            return StatusCode(StatusCodes.Status502BadGateway, $"Quiz generation failed: {exception.Message}");
        }

        if (!request.Save) return Ok(new QuizResponse { Id = string.Empty, Title = generated.Title, Blocks = generated.Blocks, CreatedAt = DateTimeOffset.UtcNow });
        var quiz = new QuizDocument { Id = ObjectId.GenerateNewId().ToString(), OwnerId = UserId(), Title = generated.Title, Blocks = generated.Blocks.ToList() };
        await quizzes.CreateAsync(quiz, cancellationToken);
        return Ok(new QuizResponse { Id = quiz.Id, Title = quiz.Title, Blocks = quiz.Blocks, CreatedAt = quiz.CreatedAt });
    }

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Authenticated user ID is missing.");
}