using System.Security.Claims;
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
    [HttpPost("quiz-from-document")]
    [RequestSizeLimit(25 * 1024 * 1024)]
    public async Task<ActionResult<QuizResponse>> Generate(IFormFile file, [FromForm] GenerateQuizRequest request, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0) return BadRequest("A document is required.");
        var settings = uploadOptions.Value;
        if (file.Length > settings.MaxBytes || !settings.AllowedContentTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase)) return BadRequest("The document type or size is not allowed.");
        await using var stream = file.OpenReadStream();
        var generated = await generator.GenerateAsync(stream, file.ContentType, request.Title, cancellationToken);
        if (!request.Save) return Ok(new QuizResponse { Id = string.Empty, Title = generated.Title, Blocks = generated.Blocks, CreatedAt = DateTimeOffset.UtcNow });
        var quiz = new QuizDocument { Id = ObjectId.GenerateNewId().ToString(), OwnerId = UserId(), Title = generated.Title, Blocks = generated.Blocks.ToList() };
        await quizzes.CreateAsync(quiz, cancellationToken);
        return Ok(new QuizResponse { Id = quiz.Id, Title = quiz.Title, Blocks = quiz.Blocks, CreatedAt = quiz.CreatedAt });
    }

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Authenticated user ID is missing.");
}