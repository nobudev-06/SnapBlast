using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using SnapBlast.Api.Models;
using SnapBlast.Api.Services;

namespace SnapBlast.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/quizzes")]
public sealed class QuizzesController(IQuizRepository quizzes) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<QuizResponse>>> List(CancellationToken cancellationToken) =>
        Ok((await quizzes.ListAsync(UserId(), cancellationToken)).Select(ToResponse).ToArray());

    [HttpGet("{id}")]
    public async Task<ActionResult<QuizResponse>> Get(string id, CancellationToken cancellationToken)
    {
        var quiz = await quizzes.GetAsync(UserId(), id, cancellationToken);
        return quiz is null ? NotFound() : Ok(ToResponse(quiz));
    }

    [HttpPost]
    public async Task<ActionResult<QuizResponse>> Create(SaveQuizRequest request, CancellationToken cancellationToken)
    {
        if (!IsValid(request)) return BadRequest("A title and at least two complete quiz pairs are required.");
        var quiz = new QuizDocument { Id = ObjectId.GenerateNewId().ToString(), OwnerId = UserId(), Title = request.Title.Trim(), Blocks = request.Blocks.ToList() };
        await quizzes.CreateAsync(quiz, cancellationToken);
        return CreatedAtAction(nameof(Get), new { id = quiz.Id }, ToResponse(quiz));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<QuizResponse>> Update(string id, SaveQuizRequest request, CancellationToken cancellationToken)
    {
        if (!IsValid(request)) return BadRequest("A title and at least two complete quiz pairs are required.");
        var quiz = await quizzes.GetAsync(UserId(), id, cancellationToken);
        if (quiz is null) return NotFound();
        quiz.Title = request.Title.Trim();
        quiz.Blocks = request.Blocks.ToList();
        quiz.UpdatedAt = DateTimeOffset.UtcNow;
        await quizzes.UpdateAsync(quiz, cancellationToken);
        return Ok(ToResponse(quiz));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken) =>
        await quizzes.DeleteAsync(UserId(), id, cancellationToken) ? NoContent() : NotFound();

    private string UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("Authenticated user ID is missing.");
    private static bool IsValid(SaveQuizRequest request) => !string.IsNullOrWhiteSpace(request.Title) && request.Blocks.Count >= 2 && request.Blocks.All(block => !string.IsNullOrWhiteSpace(block.Term) && !string.IsNullOrWhiteSpace(block.Definition));
    private static QuizResponse ToResponse(QuizDocument quiz) => new() { Id = quiz.Id, Title = quiz.Title, CreatedAt = quiz.CreatedAt, Blocks = quiz.Blocks };
}